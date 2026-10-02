using Bogus;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TroyWC_RentalManagement.Data;
using TroyWC_RentalManagement.Models;

namespace TroyWC_RentalManagement.bogus;

/// <summary>
/// Fills a Development database with test data:
/// <list type="bullet">
/// <item>the logins listed under SeedData:Users in appsettings.Development.json;</item>
/// <item>five properties, each with one unit of every bedroom count (Studio–4) and one unit in every
/// occupancy (see <see cref="Occupancy"/>), plus a soft-deleted property;</item>
/// <item>rental applications in every <see cref="AppStatus"/>, with history, comments and residences;</item>
/// <item>active and ended leases.</item>
/// </list>
/// Idempotent: users are matched by email and kept in step with appsettings; the rest is written once,
/// in a single transaction, and skipped when its properties already exist.
/// Deterministic: Bogus runs from fixed seeds against <see cref="Anchor"/> instead of the clock, and user ids
/// come from the email, so every machine gets the same data.
/// </summary>
public sealed class DevDataSeeder(
    ApplicationDbContext context,
    UserManager<IdentityUser> userManager,
    IOptions<SeedDataOptions> options,
    ILogger<DevDataSeeder> logger)
{
    private const int Seed = 20260901;

    /// <summary>Stands in for "now" in every generated date.</summary>
    private static readonly DateTimeOffset Anchor = new(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);

    private const int PropertyCount = 5;
    private const int MaxBedrooms = 4;
    private const int MaxApplicationsPerUnit = 3;

    private static readonly int[] BaseRentByBedrooms = [900, 1150, 1450, 1800, 2200];
    private static readonly string[] PropertySuffixes = ["Apartments", "Commons", "Lofts", "Terrace", "Place", "Court"];

    private enum Occupancy
    {
        /// <summary>Available, nobody has applied.</summary>
        Vacant,

        /// <summary>Available, with applications in progress.</summary>
        Applied,

        /// <summary>Approved application and an active lease; also a denied application and a complete draft
        /// whose submit will be rejected because the unit is leased.</summary>
        Leased,

        /// <summary>Approved application whose lease has ended, and a new submitted application.</summary>
        PreviouslyLeased,

        /// <summary>Soft-deleted.</summary>
        Deleted,
    }

    /// <summary>Applications on an <see cref="Occupancy.Applied"/> unit, indexed by bedroom count. Together they
    /// cover every status; the Approved one is waiting for its lease, so the unit is still available.</summary>
    private static readonly AppStatus[][] AppliedMixes =
    [
        [AppStatus.Draft, AppStatus.Submitted],
        [AppStatus.Draft, AppStatus.UnderReview, AppStatus.Withdrawn],
        [AppStatus.Returned, AppStatus.Denied, AppStatus.Submitted],
        [AppStatus.UnderReview, AppStatus.Draft, AppStatus.Approved],
        [AppStatus.Submitted, AppStatus.Returned, AppStatus.Denied],
    ];

    /// <summary>How much of a Draft application is filled in.</summary>
    private enum DraftProgress
    {
        NameOnly,
        ApplicantInfo,

        /// <summary>Both sections done; ready to submit.</summary>
        Complete,
    }

    /// <summary>Progress of the Draft in <see cref="AppliedMixes"/>, by bedroom count (rows without a Draft ignore it).</summary>
    private static readonly DraftProgress[] AppliedDraftProgress =
        [DraftProgress.NameOnly, DraftProgress.ApplicantInfo, DraftProgress.Complete, DraftProgress.Complete, DraftProgress.Complete];

    /// <summary>Status changes leading to each status, starting from Draft.</summary>
    private static readonly Dictionary<AppStatus, AppStatus[]> StatusPaths = new()
    {
        [AppStatus.Draft] = [],
        [AppStatus.Submitted] = [AppStatus.Submitted],
        [AppStatus.UnderReview] = [AppStatus.Submitted, AppStatus.UnderReview],
        [AppStatus.Returned] = [AppStatus.Submitted, AppStatus.UnderReview, AppStatus.Returned],
        [AppStatus.Approved] = [AppStatus.Submitted, AppStatus.UnderReview, AppStatus.Approved],
        [AppStatus.Denied] = [AppStatus.Submitted, AppStatus.UnderReview, AppStatus.Denied],
        [AppStatus.Withdrawn] = [AppStatus.Submitted, AppStatus.Withdrawn],
    };

    private static readonly string[] ReturnedComments =
    [
        "Please add a landlord phone number we can reach for your previous residence.",
        "Your move-in dates overlap. Please correct your residence history and resubmit.",
    ];

    private static readonly string[] DeniedComments =
    [
        "We were unable to verify your rental history.",
        "Another applicant was approved for this unit.",
    ];

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var settings = options.Value;
        if (!settings.Enabled)
            return;

        var unknownRole = settings.Users.FirstOrDefault(u => u.Role is not (Roles.Applicant or Roles.PropertyManager));
        if (unknownRole is not null)
            throw new InvalidOperationException(
                $"SeedData user {unknownRole.Email} has role '{unknownRole.Role}'; use {Roles.Applicant} or {Roles.PropertyManager}.");

        var managers = await EnsureUsersAsync(settings.Users, Roles.PropertyManager);
        var applicants = await EnsureUsersAsync(settings.Users, Roles.Applicant);

        var f = FakeData.CreateFaker(Seed);
        var properties = BuildProperties(f);
        var names = properties.Select(p => p.Property.Name).ToList();

        if (await context.Properties.AnyAsync(p => names.Contains(p.Name), ct))
        {
            logger.LogInformation("Seed data already present; only users were checked.");
            return;
        }

        if (managers.Count == 0 || applicants.Count < MaxApplicationsPerUnit)
            throw new InvalidOperationException(
                $"SeedData:Users needs at least one {Roles.PropertyManager} and {MaxApplicationsPerUnit} {Roles.Applicant} users.");

        await using var transaction = await context.Database.BeginTransactionAsync(ct);

        // Units first: ApplicationApplicant.UnitId is a plain column, so the unit ids must exist.
        context.Properties.AddRange(properties.Select(p => p.Property));
        await context.SaveChangesAsync(ct);

        var applications = new ApplicationFactory(f, applicants, managers);
        foreach (var (unit, occupancy) in properties.SelectMany(p => p.Units))
            AddApplications(f, applications, unit, occupancy);

        await context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        logger.LogInformation(
            "Seeded {Properties} properties, {Units} units, {Applications} applications and {Leases} leases.",
            properties.Count, properties.Sum(p => p.Units.Count), applications.Count, applications.LeaseCount);
    }

    /// <summary>Creates missing users, applies the configured password and role, and returns the users for <paramref name="role"/>.</summary>
    private async Task<List<IdentityUser>> EnsureUsersAsync(IEnumerable<SeedUser> seedUsers, string role)
    {
        var users = new List<IdentityUser>();

        foreach (var seedUser in seedUsers.Where(u => u.Role == role))
        {
            var user = await userManager.FindByEmailAsync(seedUser.Email);
            if (user is null)
            {
                user = new IdentityUser
                {
                    Id = FakeData.StableGuid(seedUser.Email).ToString(),
                    UserName = seedUser.Email,
                    Email = seedUser.Email,
                    EmailConfirmed = true,
                };
                ThrowIfFailed(await userManager.CreateAsync(user, seedUser.Password), seedUser.Email);
            }
            else if (!await userManager.CheckPasswordAsync(user, seedUser.Password))
            {
                if (await userManager.HasPasswordAsync(user))
                    ThrowIfFailed(await userManager.RemovePasswordAsync(user), seedUser.Email);
                ThrowIfFailed(await userManager.AddPasswordAsync(user, seedUser.Password), seedUser.Email);
            }

            if (!await userManager.IsInRoleAsync(user, role))
                ThrowIfFailed(await userManager.AddToRoleAsync(user, role), seedUser.Email);

            users.Add(user);
        }

        return users;
    }

    private static void ThrowIfFailed(IdentityResult result, string email)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException(
                $"Seeding user {email} failed: {string.Join(" ", result.Errors.Select(e => e.Description))}");
    }

    /// <summary>
    /// Five active properties plus one soft-deleted one. Unit (bedrooms b, occupancy o) goes to property (b + o) % 5,
    /// so each property gets every bedroom count once and every occupancy once.
    /// </summary>
    private static List<(Property Property, List<(Unit Unit, Occupancy Occupancy)> Units)> BuildProperties(Faker f)
    {
        var result = new List<(Property, List<(Unit, Occupancy)>)>();
        var names = new HashSet<string>();
        var occupancies = Enum.GetValues<Occupancy>();

        for (var p = 0; p <= PropertyCount; p++)
        {
            string name;
            do
                name = $"{f.Address.StreetName()} {f.PickRandom(PropertySuffixes)}";
            while (!names.Add(name));

            var created = Anchor.AddDays(-f.Random.Int(400, 1500));
            var isDeleted = p == PropertyCount;
            var property = new Property
            {
                Name = name,
                Address = FakeData.Address(f),
                Created = created,
                Updated = isDeleted ? Anchor.AddDays(-f.Random.Int(10, 90)) : null,
                IsDeleted = isDeleted,
            };

            var units = new List<(Unit, Occupancy)>();
            for (var bedrooms = 0; bedrooms <= MaxBedrooms; bedrooms++)
            {
                // The deleted property keeps two deleted units; a property can only be deleted once its units are.
                if (isDeleted && bedrooms > 1)
                    break;

                var occupancy = isDeleted ? Occupancy.Deleted : occupancies[(p - bedrooms + occupancies.Length) % occupancies.Length];
                var unit = new Unit
                {
                    UnitNumber = (101 + bedrooms).ToString(),
                    Bedrooms = bedrooms,
                    RentAmount = BaseRentByBedrooms[bedrooms] + f.Random.Int(0, 8) * 25,
                    IsDeleted = occupancy == Occupancy.Deleted,
                };
                property.Units.Add(unit);
                units.Add((unit, occupancy));
            }

            result.Add((property, units));
        }

        return result;
    }

    private void AddApplications(Faker f, ApplicationFactory applications, Unit unit, Occupancy occupancy)
    {
        switch (occupancy)
        {
            case Occupancy.Applied:
                foreach (var status in AppliedMixes[unit.Bedrooms])
                    context.RentalApplications.Add(applications.Create(
                        unit, status, Anchor.AddDays(-f.Random.Int(14, 60)), AppliedDraftProgress[unit.Bedrooms]));
                break;

            case Occupancy.Leased:
            {
                var start = FirstOfMonth(Anchor.AddMonths(-f.Random.Int(1, 10)));
                var approved = applications.Create(unit, AppStatus.Approved, ToOffset(start).AddDays(-f.Random.Int(30, 45)));
                context.RentalApplications.Add(approved);
                context.RentalApplications.Add(applications.Create(unit, AppStatus.Denied, ToOffset(start).AddDays(-f.Random.Int(30, 45))));
                context.RentalApplications.Add(applications.Create(unit, AppStatus.Draft, Anchor.AddDays(-f.Random.Int(3, 10))));
                context.Leases.Add(applications.CreateLease(approved, LeaseStatus.Active, start));
                break;
            }

            case Occupancy.PreviouslyLeased:
            {
                var start = FirstOfMonth(Anchor.AddMonths(-f.Random.Int(24, 30)));
                var approved = applications.Create(unit, AppStatus.Approved, ToOffset(start).AddDays(-f.Random.Int(30, 45)));
                context.RentalApplications.Add(approved);
                context.RentalApplications.Add(applications.Create(unit, AppStatus.Submitted, Anchor.AddDays(-f.Random.Int(14, 30))));
                context.Leases.Add(applications.CreateLease(approved, LeaseStatus.Inactive, start));
                break;
            }
        }
    }

    private static DateOnly FirstOfMonth(DateTimeOffset date) => new(date.Year, date.Month, 1);

    private static DateTimeOffset ToOffset(DateOnly date) => new(date.ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero);

    /// <summary>A seeded applicant's details, reused on every application they make.</summary>
    private sealed record Persona(
        string FirstName,
        string LastName,
        string Phone,
        Address CurrentAddress,
        IReadOnlyList<PriorResidence> PriorResidences);

    private sealed record PriorResidence(
        Address Address, string LandlordName, string LandlordPhone, DateOnly MoveIn, DateOnly MoveOut);

    /// <summary>Builds applications, handing them to applicants in turn so a unit never gets two from the same person.</summary>
    private sealed class ApplicationFactory
    {
        private readonly Faker _f;
        private readonly List<(IdentityUser User, Persona Persona)> _applicants;
        private readonly List<IdentityUser> _managers;
        private int _nextApplicant;
        private int _nextManager;

        public ApplicationFactory(Faker f, List<IdentityUser> applicants, List<IdentityUser> managers)
        {
            _f = f;
            _managers = managers;
            // Residence counts 0–3 by position, so some applicants have no prior residences.
            _applicants = applicants.Select((user, i) => (user, CreatePersona(user.Email!, i % 4))).ToList();
        }

        public int Count { get; private set; }

        public int LeaseCount { get; private set; }

        /// <param name="draftProgress">How much a Draft is filled in; every other status is complete.</param>
        public RentalApplication Create(
            Unit unit, AppStatus status, DateTimeOffset created, DraftProgress draftProgress = DraftProgress.Complete)
        {
            var (user, persona) = _applicants[_nextApplicant++ % _applicants.Count];
            Count++;

            var application = new RentalApplication
            {
                UnitId = unit.Id,
                Status = status,
                CreatedByUserId = user.Id,
                Created = created,
                Updated = created,
            };

            var applicant = new ApplicationApplicant
            {
                UserId = user.Id,
                UnitId = unit.Id,
                IsPrimary = true,
                FirstName = persona.FirstName,
                LastName = persona.LastName,
                Updated = created.UtcDateTime,
            };
            application.Applicants.Add(applicant);

            var progress = status == AppStatus.Draft ? draftProgress : DraftProgress.Complete;
            if (progress >= DraftProgress.ApplicantInfo)
            {
                applicant.Phone = persona.Phone;
                applicant.Email = user.Email;
                applicant.CurrentAddress = FakeData.Copy(persona.CurrentAddress);
                application.ApplicantInfoCompleted = true;
            }
            if (progress == DraftProgress.Complete)
            {
                foreach (var prior in persona.PriorResidences)
                {
                    applicant.Residences.Add(new Residence
                    {
                        Address = FakeData.Copy(prior.Address),
                        LandlordName = prior.LandlordName,
                        LandlordPhone = prior.LandlordPhone,
                        MoveInDate = prior.MoveIn.ToString("yyyy-MM-dd"),
                        MoveOutDate = prior.MoveOut.ToString("yyyy-MM-dd"),
                        IsCurrent = false,
                    });
                }
                application.ResidenceHistoryCompleted = true;
            }

            AddHistory(application, applicant, user, status);
            return application;
        }

        public Lease CreateLease(RentalApplication approved, LeaseStatus status, DateOnly start)
        {
            LeaseCount++;
            return new Lease
            {
                UnitId = approved.UnitId,
                Application = approved,
                Status = status,
                StartDate = start,
                EndDate = start.AddYears(1).AddDays(-1),
                Created = approved.Approved!.Value.AddDays(1),
            };
        }

        /// <summary>Walks the application from Draft to <paramref name="status"/>, recording each change.</summary>
        private void AddHistory(RentalApplication application, ApplicationApplicant applicant, IdentityUser user, AppStatus status)
        {
            var path = StatusPaths[status];
            if (path.Length == 0)
                return;

            var manager = path.Any(IsManagerDecision) ? _managers[_nextManager++ % _managers.Count] : null;
            application.AssignedManagerId = manager?.Id;

            var time = application.Created;
            var from = AppStatus.Draft;

            foreach (var to in path)
            {
                time = time.AddDays(_f.Random.Int(1, 3)).AddHours(_f.Random.Int(0, 8));
                var actor = IsManagerDecision(to) ? manager! : user;

                application.History.Add(new ApplicationHistory
                {
                    OccurredTime = time,
                    ActorUserId = actor.Id,
                    ActorRole = IsManagerDecision(to) ? Roles.PropertyManager : Roles.Applicant,
                    EventType = to.ToString(),
                    FromStatus = from.ToString(),
                    ToStatus = to.ToString(),
                });

                switch (to)
                {
                    case AppStatus.Submitted:
                        application.Submitted = time;
                        break;
                    case AppStatus.Approved:
                        application.Approved = time;
                        break;
                    case AppStatus.Withdrawn:
                        applicant.Withdrawn = time.UtcDateTime;
                        break;
                    case AppStatus.Returned:
                        AddComment(application, manager!, _f.PickRandom(ReturnedComments), time);
                        break;
                    case AppStatus.Denied:
                        AddComment(application, manager!, _f.PickRandom(DeniedComments), time);
                        break;
                }

                from = to;
            }

            application.Updated = time;
        }

        private static bool IsManagerDecision(AppStatus status) =>
            status is AppStatus.UnderReview or AppStatus.Returned or AppStatus.Approved or AppStatus.Denied;

        private static void AddComment(RentalApplication application, IdentityUser author, string body, DateTimeOffset time) =>
            application.Comments.Add(new ApplicationComment { AuthorUserId = author.Id, Body = body, Created = time });

        /// <summary>Seeded from the email, so a person's details don't depend on where they appear in appsettings.</summary>
        private static Persona CreatePersona(string email, int priorResidenceCount)
        {
            var f = FakeData.CreateFaker(FakeData.StableSeed(email));

            var priors = new List<PriorResidence>();
            var moveIn = DateOnly.FromDateTime(Anchor.UtcDateTime).AddMonths(-f.Random.Int(6, 36));
            for (var i = 0; i < priorResidenceCount; i++)
            {
                var moveOut = moveIn.AddDays(-f.Random.Int(0, 20));
                moveIn = moveOut.AddMonths(-f.Random.Int(8, 40));
                priors.Add(new PriorResidence(
                    FakeData.Address(f), f.Name.FullName(), FakeData.Phone(f), moveIn, moveOut));
            }

            return new Persona(f.Name.FirstName(), f.Name.LastName(), FakeData.Phone(f), FakeData.Address(f), priors);
        }
    }
}
