using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using TroyWC_RentalManagement.Areas.Applicant.Models;
using TroyWC_RentalManagement.Data;
using TroyWC_RentalManagement.Models;

namespace TroyWC_RentalManagement.Areas.Applicant.Controllers;

/// <summary>
/// The applicant's rental application wizard: applicant info → residence history → review and submit.
/// Continue validates the posted section and saves only when it is valid; Save always saves;
/// Back is a plain link, so unsaved changes are dropped.
/// </summary>
[Area("Applicant")]
[Route("Applicant/[controller]")]
[Authorize(Roles = Roles.Applicant)]
[AutoValidateAntiforgeryToken]
public class ApplicationsController(ApplicationDbContext context, UserManager<IdentityUser> userManager) : Controller
{
    /// <summary>Statuses where Apply sends the applicant back to their existing application instead of starting another.</summary>
    private static readonly AppStatus[] OpenStatuses =
        [AppStatus.Draft, AppStatus.Returned, AppStatus.Submitted, AppStatus.UnderReview];

    private string UserId => userManager.GetUserId(User)!;

    [HttpGet("Apply/{unitId:int}")]
    public async Task<IActionResult> Apply(int unitId, CancellationToken ct)
    {
        if (await FindOpenApplicationIdAsync(unitId, ct) is int existingId)
            return RedirectToAction(nameof(Applicant), new { id = existingId });

        var unit = await FindAvailableUnitAsync(unitId, ct);
        if (unit is null)
            return UnitUnavailable();

        var user = await userManager.GetUserAsync(User);

        return Wizard(new RentalApplicationViewModel
        {
            UnitId = unitId,
            Unit = UnitSummary.FromEntity(unit),
            Step = ApplicationStep.Applicant,
            Applicant = new ApplicantInfoSection { Email = user?.Email, Phone = user?.PhoneNumber },
        });
    }

    /// <summary>Step 1 for a new application: the first Continue or Save creates it.</summary>
    [HttpPost("Apply/{unitId:int}")]
    public async Task<IActionResult> Apply(int unitId, RentalApplicationViewModel model, CancellationToken ct)
    {
        // Another tab may have started one already.
        if (await FindOpenApplicationIdAsync(unitId, ct) is int existingId)
        {
            TempData[FlashMessages.Error] = "You already have an application for this unit. Your changes were not saved.";
            return RedirectToAction(nameof(Applicant), new { id = existingId });
        }

        var unit = await FindAvailableUnitAsync(unitId, ct);
        if (unit is null)
            return UnitUnavailable();

        ModelState.Clear();
        var valid = IsSectionValid(model.Applicant, nameof(model.Applicant));

        if (model.Command == WizardCommand.Continue && !valid)
        {
            model.UnitId = unitId;
            model.Unit = UnitSummary.FromEntity(unit);
            model.Step = ApplicationStep.Applicant;
            return ContinueFailed(model);
        }

        var now = DateTimeOffset.UtcNow;
        var application = new RentalApplication
        {
            UnitId = unitId,
            Status = AppStatus.Draft,
            CreatedByUserId = UserId,
            Created = now,
            Updated = now,
            ApplicantInfoCompleted = model.Command == WizardCommand.Continue,
        };

        var applicant = NewPrimaryApplicant(application);
        model.Applicant.ApplyTo(applicant);
        context.RentalApplications.Add(application);
        await context.SaveChangesAsync(ct);

        return model.Command == WizardCommand.Continue
            ? RedirectToAction(nameof(Residences), new { id = application.Id })
            : Saved(nameof(Applicant), application.Id);
    }

    [HttpGet("{id:int}/Applicant")]
    public Task<IActionResult> Applicant(int id, CancellationToken ct) =>
        ShowStepAsync(id, ApplicationStep.Applicant, ct);

    [HttpPost("{id:int}/Applicant")]
    public async Task<IActionResult> Applicant(int id, RentalApplicationViewModel posted, CancellationToken ct)
    {
        var application = await LoadApplicationAsync(id, tracked: true, ct);
        if (application is null)
            return NotFound();

        if (!IsEditable(application))
            return ReadOnly(nameof(Applicant), id);

        ModelState.Clear();
        var valid = IsSectionValid(posted.Applicant, nameof(posted.Applicant));

        if (posted.Command == WizardCommand.Continue && !valid)
        {
            var model = RentalApplicationViewModel.FromEntity(application, ApplicationStep.Applicant);
            model.RowVersion = posted.RowVersion;
            model.Applicant = posted.Applicant;
            return ContinueFailed(model);
        }

        var applicant = application.Applicants.FirstOrDefault(a => a.IsPrimary) ?? NewPrimaryApplicant(application);
        posted.Applicant.ApplyTo(applicant);
        applicant.Updated = DateTime.UtcNow;

        application.ApplicantInfoCompleted = posted.Command == WizardCommand.Continue
            || (application.ApplicantInfoCompleted && valid);

        if (!await TrySaveAsync(application, posted.RowVersion, ct))
            return ChangedElsewhere(nameof(Applicant), id);

        return posted.Command == WizardCommand.Continue
            ? RedirectToAction(nameof(Residences), new { id })
            : Saved(nameof(Applicant), id);
    }

    [HttpGet("{id:int}/Residences")]
    public Task<IActionResult> Residences(int id, CancellationToken ct) =>
        ShowStepAsync(id, ApplicationStep.Residences, ct);

    [HttpPost("{id:int}/Residences")]
    public async Task<IActionResult> Residences(int id, RentalApplicationViewModel posted, CancellationToken ct)
    {
        var application = await LoadApplicationAsync(id, tracked: true, ct);
        if (application is null)
            return NotFound();

        if (!IsEditable(application))
            return ReadOnly(nameof(Residences), id);

        ModelState.Clear();

        // Re-render with the residences as posted; nothing is saved until Continue or Save.
        var model = RentalApplicationViewModel.FromEntity(application, ApplicationStep.Residences);
        model.RowVersion = posted.RowVersion;
        model.Residences = posted.Residences;

        switch (posted.Command)
        {
            case WizardCommand.SaveResidence:
                if (IsSectionValid(posted.ResidenceDraft, nameof(posted.ResidenceDraft)))
                {
                    if (posted.ResidenceDraftIndex is int index && index >= 0 && index < model.Residences.Count)
                        model.Residences[index] = posted.ResidenceDraft;
                    else
                        model.Residences.Add(posted.ResidenceDraft);
                }
                else
                {
                    model.ResidenceDraft = posted.ResidenceDraft;
                    model.ResidenceDraftIndex = posted.ResidenceDraftIndex;
                    model.ShowResidenceModal = true;
                }
                return Wizard(model);

            case WizardCommand.RemoveResidence:
                if (posted.CommandIndex is int removeAt && removeAt >= 0 && removeAt < model.Residences.Count)
                    model.Residences.RemoveAt(removeAt);
                return Wizard(model);
        }

        var valid = IsSectionValid(posted.Residences, nameof(posted.Residences));
        if (posted.Command == WizardCommand.Continue && !valid)
            return ContinueFailed(model);

        var applicant = application.Applicants.FirstOrDefault(a => a.IsPrimary) ?? NewPrimaryApplicant(application);
        SyncResidences(applicant, posted.Residences);
        applicant.Updated = DateTime.UtcNow;

        application.ResidenceHistoryCompleted = posted.Command == WizardCommand.Continue
            || (application.ResidenceHistoryCompleted && valid);

        if (!await TrySaveAsync(application, posted.RowVersion, ct))
            return ChangedElsewhere(nameof(Residences), id);

        return posted.Command == WizardCommand.Continue
            ? RedirectToAction(nameof(Review), new { id })
            : Saved(nameof(Residences), id);
    }

    [HttpGet("{id:int}/Review")]
    public Task<IActionResult> Review(int id, CancellationToken ct) =>
        ShowStepAsync(id, ApplicationStep.Review, ct);

    [HttpPost("{id:int}/Submit")]
    public async Task<IActionResult> Submit(int id, string? rowVersion, CancellationToken ct)
    {
        var application = await LoadApplicationAsync(id, tracked: true, ct);
        if (application is null)
            return NotFound();

        if (!IsEditable(application))
            return ReadOnly(nameof(Review), id);

        ModelState.Clear();
        var model = RentalApplicationViewModel.FromEntity(application, ApplicationStep.Review);
        model.RowVersion = rowVersion;

        var unitAvailable = await context.Units
            .Where(u => u.Id == application.UnitId)
            .Where(UnitQueries.IsAvailable)
            .AnyAsync(ct);

        if (!unitAvailable)
        {
            ModelState.AddModelError(string.Empty,
                "This unit has been leased and is no longer accepting applications.");
            return Wizard(model);
        }

        // Re-check the stored data too, not just the flags.
        if (!application.ApplicantInfoCompleted || !IsSectionValid(model.Applicant, nameof(model.Applicant)))
            ModelState.AddModelError(string.Empty, "Complete the applicant information section before submitting.");

        if (!application.ResidenceHistoryCompleted || !IsSectionValid(model.Residences, nameof(model.Residences)))
            ModelState.AddModelError(string.Empty, "Complete the residence history section before submitting.");

        if (!ModelState.IsValid)
            return Wizard(model);

        var now = DateTimeOffset.UtcNow;
        application.History.Add(new ApplicationHistory
        {
            OccurredTime = now,
            ActorUserId = UserId,
            ActorRole = Roles.Applicant,
            EventType = "Submitted",
            FromStatus = application.Status.ToString(),
            ToStatus = nameof(AppStatus.Submitted),
        });
        application.Status = AppStatus.Submitted;
        application.Submitted = now;

        if (!await TrySaveAsync(application, rowVersion, ct))
            return ChangedElsewhere(nameof(Review), id);

        TempData[FlashMessages.Success] = "Your application has been submitted.";
        return RedirectToAction(nameof(Review), new { id });
    }

    private async Task<IActionResult> ShowStepAsync(int id, ApplicationStep step, CancellationToken ct)
    {
        var application = await LoadApplicationAsync(id, tracked: false, ct);
        return application is null
            ? NotFound()
            : Wizard(RentalApplicationViewModel.FromEntity(application, step));
    }

    private ViewResult Wizard(RentalApplicationViewModel model) => View("Wizard", model);

    private ViewResult ContinueFailed(RentalApplicationViewModel model)
    {
        ModelState.AddModelError(string.Empty, "Fix the highlighted fields to continue.");
        return Wizard(model);
    }

    /// <summary>
    /// Validates one section of the view model, adding its errors to ModelState under <paramref name="prefix"/>.
    /// Call after ModelState.Clear() so only the posted section is checked.
    /// </summary>
    private bool IsSectionValid(object section, string prefix)
    {
        TryValidateModel(section, prefix);
        return ModelState.GetFieldValidationState(prefix) != ModelValidationState.Invalid;
    }

    /// <summary>The current user's application with its unit and primary applicant's residences.</summary>
    private Task<RentalApplication?> LoadApplicationAsync(int id, bool tracked, CancellationToken ct)
    {
        var query = context.RentalApplications
            .Include(a => a.Unit).ThenInclude(u => u.Property)
            .Include(a => a.Applicants.Where(p => p.IsPrimary)).ThenInclude(p => p.Residences)
            .AsSplitQuery()
            .Where(a => a.Id == id && a.CreatedByUserId == UserId);

        if (!tracked)
            query = query.AsNoTracking();

        return query.FirstOrDefaultAsync(ct);
    }

    private Task<int?> FindOpenApplicationIdAsync(int unitId, CancellationToken ct) =>
        context.RentalApplications
            .Where(a => a.UnitId == unitId && a.CreatedByUserId == UserId && OpenStatuses.Contains(a.Status))
            .OrderByDescending(a => a.Created)
            .Select(a => (int?)a.Id)
            .FirstOrDefaultAsync(ct);

    private Task<Unit?> FindAvailableUnitAsync(int unitId, CancellationToken ct) =>
        context.Units
            .AsNoTracking()
            .Include(u => u.Property)
            .Where(u => u.Id == unitId)
            .Where(UnitQueries.IsAvailable)
            .FirstOrDefaultAsync(ct);

    private ApplicationApplicant NewPrimaryApplicant(RentalApplication application)
    {
        var applicant = new ApplicationApplicant
        {
            UserId = UserId,
            UnitId = application.UnitId,
            IsPrimary = true,
            FirstName = string.Empty,
            LastName = string.Empty,
            Updated = DateTime.UtcNow,
        };
        application.Applicants.Add(applicant);
        return applicant;
    }

    /// <summary>Updates the stored residences in order, adding or removing rows to match <paramref name="inputs"/>.</summary>
    private void SyncResidences(ApplicationApplicant applicant, List<ResidenceInput> inputs)
    {
        var existing = applicant.Residences.OrderBy(r => r.Id).ToList();

        for (var i = 0; i < inputs.Count; i++)
        {
            var residence = i < existing.Count ? existing[i] : new Residence();
            inputs[i].ApplyTo(residence);
            if (i >= existing.Count)
                applicant.Residences.Add(residence);
        }

        context.Residences.RemoveRange(existing.Skip(inputs.Count));
    }

    private static bool IsEditable(RentalApplication application) =>
        application.Status is AppStatus.Draft or AppStatus.Returned;

    /// <summary>Saves, checking that nobody changed the application since the page loaded.</summary>
    private async Task<bool> TrySaveAsync(RentalApplication application, string? rowVersion, CancellationToken ct)
    {
        application.Updated = DateTimeOffset.UtcNow;

        if (TryDecodeRowVersion(rowVersion) is byte[] original)
            context.Entry(application).Property(a => a.RowVersion).OriginalValue = original;

        try
        {
            await context.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }

    private static byte[]? TryDecodeRowVersion(string? rowVersion)
    {
        if (string.IsNullOrEmpty(rowVersion))
            return null;

        var buffer = new byte[rowVersion.Length];
        return Convert.TryFromBase64String(rowVersion, buffer, out var length) ? buffer[..length] : null;
    }

    private RedirectToActionResult Saved(string action, int id)
    {
        TempData[FlashMessages.Success] = "Your application has been saved.";
        return RedirectToAction(action, new { id });
    }

    private RedirectToActionResult ReadOnly(string action, int id)
    {
        TempData[FlashMessages.Error] = "This application can no longer be changed.";
        return RedirectToAction(action, new { id });
    }

    private RedirectToActionResult ChangedElsewhere(string action, int id)
    {
        TempData[FlashMessages.Error] =
            "This application was changed in another window, so your changes were not saved. Here is the latest version.";
        return RedirectToAction(action, new { id });
    }

    private RedirectToActionResult UnitUnavailable()
    {
        TempData[FlashMessages.Error] = "That unit is no longer available.";
        return RedirectToAction("Index", "Home", new { area = "" });
    }
}
