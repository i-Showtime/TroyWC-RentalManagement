namespace TroyWC_RentalManagement.Areas.Manage.Models;

/// <summary>Model for Areas/Manage/Views/Shared/_DeleteConfirm.cshtml.</summary>
public class DeleteViewModel
{
    public int Id { get; init; }

    /// <summary>Lower-case entity name for the modal text, e.g. "property".</summary>
    public required string EntityName { get; init; }

    public required string DisplayName { get; init; }

    /// <summary>Why the record can't be deleted; null when it can.</summary>
    public string? BlockedReason { get; init; }
}
