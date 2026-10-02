using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using TroyWC_RentalManagement.Models;

namespace TroyWC_RentalManagement.Areas.Applicant.Models;

/// <summary>
/// Drives the whole rental application wizard. Each step's view component renders its own section;
/// the controller validates only the section being posted.
/// </summary>
public class RentalApplicationViewModel
{
    /// <summary>Null until the first Continue or Save creates the application.</summary>
    public int? Id { get; set; }

    /// <summary>Base64 <see cref="RentalApplication.RowVersion"/> the page was loaded with.</summary>
    public string? RowVersion { get; set; }

    public ApplicantInfoSection Applicant { get; set; } = new();

    public List<ResidenceInput> Residences { get; set; } = [];

    /// <summary>The residence in the modal.</summary>
    public ResidenceInput ResidenceDraft { get; set; } = new();

    /// <summary>Index in <see cref="Residences"/> the modal is editing; null adds a new one.</summary>
    public int? ResidenceDraftIndex { get; set; }

    public WizardCommand Command { get; set; }

    /// <summary>Residence index for <see cref="WizardCommand.RemoveResidence"/>.</summary>
    public int? CommandIndex { get; set; }

    // Everything below is loaded on the server, never bound from the form.

    [BindNever, ValidateNever]
    public int UnitId { get; set; }

    [BindNever, ValidateNever]
    public UnitSummary Unit { get; set; } = null!;

    [BindNever, ValidateNever]
    public ApplicationStep Step { get; set; }

    [BindNever, ValidateNever]
    public AppStatus Status { get; set; } = AppStatus.Draft;

    [BindNever, ValidateNever]
    public bool ApplicantInfoCompleted { get; set; }

    [BindNever, ValidateNever]
    public bool ResidenceHistoryCompleted { get; set; }

    /// <summary>The property manager's latest comment, shown while the application is Returned or Denied.</summary>
    [BindNever, ValidateNever]
    public string? ManagerMessage { get; set; }

    /// <summary>Reopens the residence modal, e.g. to show its errors.</summary>
    [BindNever, ValidateNever]
    public bool ShowResidenceModal { get; set; }

    /// <summary>Applicants can only change Draft or Returned applications.</summary>
    public bool IsEditable => Status is AppStatus.Draft or AppStatus.Returned;

    public bool CanSubmit => IsEditable && Id is not null && ApplicantInfoCompleted && ResidenceHistoryCompleted;

    public string StatusLabel => Status.ToLabel();

    /// <param name="application">Loaded with Unit.Property, Comments and the primary applicant's residences.</param>
    public static RentalApplicationViewModel FromEntity(RentalApplication application, ApplicationStep step)
    {
        var applicant = application.Applicants.FirstOrDefault(a => a.IsPrimary);

        return new RentalApplicationViewModel
        {
            Id = application.Id,
            RowVersion = Convert.ToBase64String(application.RowVersion),
            Applicant = applicant is null ? new() : ApplicantInfoSection.FromEntity(applicant),
            Residences = applicant?.Residences
                .OrderBy(r => r.Id)
                .Select(ResidenceInput.FromEntity)
                .ToList() ?? [],
            UnitId = application.UnitId,
            Unit = UnitSummary.FromEntity(application.Unit),
            Step = step,
            Status = application.Status,
            ApplicantInfoCompleted = application.ApplicantInfoCompleted,
            ResidenceHistoryCompleted = application.ResidenceHistoryCompleted,
            ManagerMessage = application.Status is AppStatus.Returned or AppStatus.Denied
                ? application.Comments.OrderByDescending(c => c.Created).Select(c => c.Body).FirstOrDefault()
                : null,
        };
    }
}

/// <summary>The unit being applied for, shown at the top of every step.</summary>
public sealed record UnitSummary(string PropertyName, string UnitNumber, int Bedrooms, decimal RentAmount)
{
    /// <param name="unit">Loaded with its Property.</param>
    public static UnitSummary FromEntity(Unit unit) =>
        new(unit.Property.Name, unit.UnitNumber, unit.Bedrooms, unit.RentAmount);

    public string BedroomsLabel => Bedrooms switch
    {
        0 => "Studio",
        1 => "1 bedroom",
        _ => $"{Bedrooms} bedrooms",
    };
}
