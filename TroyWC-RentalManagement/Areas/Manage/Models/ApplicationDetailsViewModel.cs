using System.ComponentModel.DataAnnotations;
using TroyWC_RentalManagement.Areas.Applicant.Models;
using TroyWC_RentalManagement.Models;

namespace TroyWC_RentalManagement.Areas.Manage.Models;

/// <summary>A submitted application as a property manager reviews it, with the decisions available.</summary>
public class ApplicationDetailsViewModel
{
    /// <summary>The applicant's answers, shown with Views/Shared/_ApplicationSummary.cshtml.</summary>
    public required RentalApplicationViewModel Application { get; init; }

    public string? AssignedManager { get; init; }

    public required IReadOnlyList<CommentItem> Comments { get; init; }

    public required IReadOnlyList<HistoryItem> History { get; init; }

    /// <summary>The lease created when this application was approved.</summary>
    public LeaseSummary? Lease { get; init; }

    public DecisionInput Decision { get; set; } = new();

    public int Id => Application.Id!.Value;

    public AppStatus Status => Application.Status;

    public bool CanStartReview => ApplicationWorkflow.CanMove(Status, AppStatus.UnderReview);

    public bool CanReturn => ApplicationWorkflow.CanMove(Status, AppStatus.Returned);

    public bool CanDeny => ApplicationWorkflow.CanMove(Status, AppStatus.Denied);

    public bool CanApprove => ApplicationWorkflow.CanMove(Status, AppStatus.Approved);

    public bool HasDecisions => CanStartReview || CanReturn || CanDeny || CanApprove;
}

public sealed record CommentItem(string Author, string Body, DateTimeOffset Created);

public sealed record HistoryItem(DateTimeOffset OccurredTime, string Actor, string ActorRole, string From, string To);

public sealed record LeaseSummary(LeaseStatus Status, DateOnly StartDate, DateOnly? EndDate);

/// <summary>Posted by every decision button on the application page (prefix "Decision").</summary>
public class DecisionInput
{
    public string? RowVersion { get; set; }

    /// <summary>Required to return an application; optional otherwise. Shown to the applicant.</summary>
    [MaxLength(4000)]
    [Display(Name = "Comment to the applicant")]
    public string? Comment { get; set; }

    [Display(Name = "Lease start")]
    public DateOnly? LeaseStart { get; set; }

    [Display(Name = "Lease end")]
    public DateOnly? LeaseEnd { get; set; }
}
