using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TroyWC_RentalManagement.Areas.Applicant.Models;
using TroyWC_RentalManagement.Areas.Manage.Models;
using TroyWC_RentalManagement.Data;
using TroyWC_RentalManagement.Grid;
using TroyWC_RentalManagement.Models;

namespace TroyWC_RentalManagement.Areas.Manage.Controllers;

/// <summary>
/// Property managers review submitted applications: start review, return to the applicant, deny, or approve.
/// Approving creates the unit's lease. Drafts are visible but can't be decided on until the applicant submits them.
/// </summary>
[Area("Manage")]
[Route("Manage/[controller]")]
[Authorize(Roles = Roles.PropertyManager)]
[AutoValidateAntiforgeryToken]
public class ApplicationsController(ApplicationDbContext context, UserManager<IdentityUser> userManager) : Controller
{
    private string UserId => userManager.GetUserId(User)!;

    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] GridRequest grid, AppStatus? status, CancellationToken ct)
    {
        var query = context.RentalApplications.AsNoTracking();

        if (status is not null)
            query = query.Where(a => a.Status == status);

        var routeValues = status is null
            ? null
            : new Dictionary<string, string> { ["status"] = status.Value.ToString() };

        var gridModel = await query
            .Select(a => new ApplicationRow
            {
                Id = a.Id,
                ApplicantName = a.Applicants
                    .Where(p => p.IsPrimary)
                    .Select(p => p.FirstName + " " + p.LastName)
                    .FirstOrDefault() ?? string.Empty,
                PropertyName = a.Unit.Property.Name,
                UnitNumber = a.Unit.UnitNumber,
                Status = a.Status,
                Submitted = a.Submitted,
            })
            .ToGridAsync(BuildGrid(), grid, routeValues, ct);

        if (Request.IsAjax())
            return PartialView("_Grid", gridModel);

        return View(new ApplicationsIndexViewModel
        {
            Grid = gridModel,
            Status = status,
            Statuses = Enum.GetValues<AppStatus>()
                .Select(s => new SelectListItem(s.ToLabel(), s.ToString())),
        });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var model = await BuildDetailsAsync(id, ct);
        if (model is null)
            return NotFound();

        var start = FirstOfNextMonth();
        model.Decision = new DecisionInput
        {
            RowVersion = model.Application.RowVersion,
            LeaseStart = start,
            LeaseEnd = start.AddYears(1).AddDays(-1),
        };

        return View(model);
    }

    [HttpPost("{id:int}/StartReview")]
    public Task<IActionResult> StartReview(int id, [Bind(Prefix = "Decision")] DecisionInput input, CancellationToken ct) =>
        DecideAsync(id, AppStatus.UnderReview, input, ct);

    [HttpPost("{id:int}/Return")]
    public Task<IActionResult> Return(int id, [Bind(Prefix = "Decision")] DecisionInput input, CancellationToken ct) =>
        DecideAsync(id, AppStatus.Returned, input, ct);

    [HttpPost("{id:int}/Deny")]
    public Task<IActionResult> Deny(int id, [Bind(Prefix = "Decision")] DecisionInput input, CancellationToken ct) =>
        DecideAsync(id, AppStatus.Denied, input, ct);

    /// <summary>Approves the application and creates an active lease for its unit.</summary>
    [HttpPost("{id:int}/Approve")]
    public Task<IActionResult> Approve(int id, [Bind(Prefix = "Decision")] DecisionInput input, CancellationToken ct) =>
        DecideAsync(id, AppStatus.Approved, input, ct);

    private async Task<IActionResult> DecideAsync(int id, AppStatus to, DecisionInput input, CancellationToken ct)
    {
        var application = await context.RentalApplications
            .FirstOrDefaultAsync(a => a.Id == id, ct);
        if (application is null)
            return NotFound();

        if (!ApplicationWorkflow.CanMove(application.Status, to))
        {
            TempData[FlashMessages.Error] =
                $"This application is {application.Status.ToLabel().ToLowerInvariant()}, so it can't be moved to {to.ToLabel().ToLowerInvariant()}.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var comment = string.IsNullOrWhiteSpace(input.Comment) ? null : input.Comment.Trim();

        if (to == AppStatus.Returned && comment is null)
            ModelState.AddModelError("Decision.Comment", "Tell the applicant what to change before returning the application.");

        if (to == AppStatus.Approved)
            await ValidateLeaseAsync(application.UnitId, input, ct);

        if (!ModelState.IsValid)
            return await RedisplayAsync(id, input, ct);

        var now = DateTimeOffset.UtcNow;
        ApplicationWorkflow.Move(application, to, UserId, Roles.PropertyManager, now);
        application.AssignedManagerId ??= UserId;

        if (comment is not null)
            application.Comments.Add(new ApplicationComment { AuthorUserId = UserId, Body = comment, Created = now });

        if (to == AppStatus.Approved)
        {
            context.Leases.Add(new Lease
            {
                UnitId = application.UnitId,
                Application = application,
                Status = LeaseStatus.Active,
                StartDate = input.LeaseStart!.Value,
                EndDate = input.LeaseEnd!.Value,
                Created = now,
            });
        }

        if (TryDecodeRowVersion(input.RowVersion) is byte[] original)
            context.Entry(application).Property(a => a.RowVersion).OriginalValue = original;

        try
        {
            await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            TempData[FlashMessages.Error] =
                "This application was changed by someone else, so your decision was not saved. Here is the latest version.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (DbUpdateException ex) when (IsActiveLeaseConflict(ex))
        {
            // Another application for this unit was approved at the same moment.
            context.ChangeTracker.Clear();
            ModelState.AddModelError(string.Empty, "This unit already has an active lease.");
            return await RedisplayAsync(id, input, ct);
        }

        TempData[FlashMessages.Success] = to switch
        {
            AppStatus.UnderReview => "Review started.",
            AppStatus.Returned => "The application was returned to the applicant.",
            AppStatus.Denied => "The application was denied.",
            _ => "The application was approved and its lease was created.",
        };
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>Lease dates must be set and in order, and the unit must still be free to lease.</summary>
    private async Task ValidateLeaseAsync(int unitId, DecisionInput input, CancellationToken ct)
    {
        if (input.LeaseStart is null)
            ModelState.AddModelError("Decision.LeaseStart", "Enter the lease start date.");

        if (input.LeaseEnd is null)
            ModelState.AddModelError("Decision.LeaseEnd", "Enter the lease end date.");
        else if (input.LeaseStart is not null && input.LeaseEnd <= input.LeaseStart)
            ModelState.AddModelError("Decision.LeaseEnd", "The lease must end after it starts.");

        var unit = await context.Units
            .Where(u => u.Id == unitId)
            .Select(u => new
            {
                Deleted = u.IsDeleted || u.Property.IsDeleted,
                Leased = u.Leases.Any(l => l.Status == LeaseStatus.Active),
            })
            .FirstAsync(ct);

        if (unit.Leased)
            ModelState.AddModelError(string.Empty, "This unit already has an active lease.");
        else if (unit.Deleted)
            ModelState.AddModelError(string.Empty, "This unit has been deleted and can't be leased.");
    }

    private async Task<IActionResult> RedisplayAsync(int id, DecisionInput input, CancellationToken ct)
    {
        var model = await BuildDetailsAsync(id, ct);
        if (model is null)
            return NotFound();

        model.Decision = input;
        return View(nameof(Details), model);
    }

    private async Task<ApplicationDetailsViewModel?> BuildDetailsAsync(int id, CancellationToken ct)
    {
        var application = await context.RentalApplications
            .AsNoTracking()
            .Include(a => a.Unit).ThenInclude(u => u.Property)
            .Include(a => a.Applicants.Where(p => p.IsPrimary)).ThenInclude(p => p.Residences)
            .Include(a => a.Comments)
            .Include(a => a.History)
            .Include(a => a.Leases)
            .AsSplitQuery()
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (application is null)
            return null;

        var userIds = application.Comments.Select(c => c.AuthorUserId)
            .Concat(application.History.Select(h => h.ActorUserId))
            .Append(application.AssignedManagerId)
            .OfType<string>()
            .Distinct()
            .ToList();

        var emails = await context.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Email ?? u.UserName ?? u.Id, ct);

        string Name(string? userId) => userId is not null && emails.TryGetValue(userId, out var email) ? email : "Unknown user";

        var lease = application.Leases.OrderByDescending(l => l.Created).FirstOrDefault();

        return new ApplicationDetailsViewModel
        {
            Application = RentalApplicationViewModel.FromEntity(application, ApplicationStep.Review),
            AssignedManager = application.AssignedManagerId is null ? null : Name(application.AssignedManagerId),
            Comments = application.Comments
                .OrderBy(c => c.Created)
                .Select(c => new CommentItem(Name(c.AuthorUserId), c.Body, c.Created))
                .ToList(),
            History = application.History
                .OrderBy(h => h.OccurredTime)
                .Select(h => new HistoryItem(h.OccurredTime, Name(h.ActorUserId), h.ActorRole, StatusLabel(h.FromStatus), StatusLabel(h.ToStatus)))
                .ToList(),
            Lease = lease is null ? null : new LeaseSummary(lease.Status, lease.StartDate, lease.EndDate),
        };
    }

    private static string StatusLabel(string? status) =>
        Enum.TryParse<AppStatus>(status, out var parsed) ? parsed.ToLabel() : status ?? string.Empty;

    private static DateOnly FirstOfNextMonth()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return new DateOnly(today.Year, today.Month, 1).AddMonths(1);
    }

    private static bool IsActiveLeaseConflict(DbUpdateException ex) =>
        ex.InnerException is SqlException { Number: 2601 or 2627 } sql
        && sql.Message.Contains("IX_Leases_UnitId_Active", StringComparison.Ordinal);

    private static byte[]? TryDecodeRowVersion(string? rowVersion)
    {
        if (string.IsNullOrEmpty(rowVersion))
            return null;

        var buffer = new byte[rowVersion.Length];
        return Convert.TryFromBase64String(rowVersion, buffer, out var length) ? buffer[..length] : null;
    }

    private GridDefinition<ApplicationRow> BuildGrid() => new()
    {
        Columns =
        [
            GridColumn<ApplicationRow>.For(r => r.ApplicantName, "Applicant"),
            GridColumn<ApplicationRow>.For(r => r.PropertyName, "Property"),
            GridColumn<ApplicationRow>.For(r => r.UnitNumber, "Unit #", sortKey: "unit"),
            GridColumn<ApplicationRow>.For(r => r.Status, "Status", display: s => s.ToLabel()),
            GridColumn<ApplicationRow>.For(r => r.Submitted, "Submitted",
                display: d => d?.ToString("MMM d, yyyy") ?? string.Empty),
        ],
        Key = r => r.Id,
        DefaultSort = "submitted",
        DefaultDesc = true,
        Actions =
        [
            new("Review", r => Url.Action(nameof(Details), new { id = r.Id })!,
                "btn btn-sm btn-outline-primary", OpensModal: false),
        ],
        EmptyMessage = "No applications to review.",
    };
}
