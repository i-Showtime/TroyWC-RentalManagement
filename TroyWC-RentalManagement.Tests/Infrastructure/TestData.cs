using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TroyWC_RentalManagement.Data;
using TroyWC_RentalManagement.Models;

namespace TroyWC_RentalManagement.Tests.Infrastructure;

public sealed record TestUser(string Id, string Email, string Password);

/// <summary>Arranges data for a test. Everything it creates is unique, so tests sharing the database don't collide.</summary>
public sealed class TestData(RentalAppFactory factory)
{
    private const string Password = "Passw0rd!";

    public async Task<TestUser> CreateUserAsync(string role)
    {
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        var email = $"{role.ToLowerInvariant()}-{Guid.NewGuid():N}@tests.test";
        var user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
        AssertSucceeded(await users.CreateAsync(user, Password));
        AssertSucceeded(await users.AddToRoleAsync(user, role));

        return new TestUser(user.Id, email, Password);
    }

    /// <summary>Signs <paramref name="user"/> in through the real login page and returns their client.</summary>
    public async Task<AppClient> LoginAsync(TestUser user)
    {
        var client = factory.CreateAppClient();
        var page = await client.GetAsync("/Identity/Account/Login");
        var response = await client.PostAsync("/Identity/Account/Login", page, new Dictionary<string, string?>
        {
            ["Input.Email"] = user.Email,
            ["Input.Password"] = user.Password,
            ["Input.RememberMe"] = "false",
        });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        return client;
    }

    public async Task<(TestUser User, AppClient Client)> SignInNewAsync(string role)
    {
        var user = await CreateUserAsync(role);
        return (user, await LoginAsync(user));
    }

    /// <summary>A new property with one available unit; returns the unit id.</summary>
    public async Task<int> CreateUnitAsync(int bedrooms = 2, decimal rent = 1500m)
    {
        await using var db = factory.CreateDbContext();
        var unit = new Unit { UnitNumber = "101", Bedrooms = bedrooms, RentAmount = rent };
        db.Properties.Add(new Property
        {
            Name = $"Test Property {Guid.NewGuid():N}",
            Address = new Address { Line1 = "1 Test St", City = "Troy", State = "NY", PostalCode = "12180" },
            Created = DateTimeOffset.UtcNow,
            Units = { unit },
        });
        await db.SaveChangesAsync();
        return unit.Id;
    }

    /// <summary>
    /// Leases <paramref name="unitId"/> behind the app's back, as if another applicant had been approved:
    /// an Approved application by a new applicant plus a lease in <paramref name="status"/>.
    /// </summary>
    public async Task<int> CreateLeaseAsync(int unitId, LeaseStatus status = LeaseStatus.Active)
    {
        var tenant = await CreateUserAsync(Roles.Applicant);
        await using var db = factory.CreateDbContext();

        var now = DateTimeOffset.UtcNow;
        var application = new RentalApplication
        {
            UnitId = unitId,
            Status = AppStatus.Approved,
            CreatedByUserId = tenant.Id,
            Created = now,
            Submitted = now,
            Approved = now,
        };
        var start = new DateOnly(2025, 1, 1);
        var lease = new Lease
        {
            UnitId = unitId,
            Application = application,
            Status = status,
            StartDate = start,
            EndDate = start.AddYears(1).AddDays(-1),
            Created = now,
        };
        db.Leases.Add(lease);
        await db.SaveChangesAsync();
        return lease.Id;
    }

    public async Task<T> QueryAsync<T>(Func<ApplicationDbContext, Task<T>> query)
    {
        await using var db = factory.CreateDbContext();
        return await query(db);
    }

    public Task<RentalApplication> LoadApplicationAsync(int id) =>
        QueryAsync(db => db.RentalApplications
            .AsNoTracking()
            .Include(a => a.Applicants).ThenInclude(p => p.Residences)
            .Include(a => a.History)
            .Include(a => a.Comments)
            .Include(a => a.Leases)
            .AsSplitQuery()
            .SingleAsync(a => a.Id == id));

    public Task<List<Lease>> LeasesForUnitAsync(int unitId) =>
        QueryAsync(db => db.Leases.AsNoTracking().Where(l => l.UnitId == unitId).ToListAsync());

    private static void AssertSucceeded(IdentityResult result) =>
        Assert.True(result.Succeeded, string.Join(" ", result.Errors.Select(e => e.Description)));
}
