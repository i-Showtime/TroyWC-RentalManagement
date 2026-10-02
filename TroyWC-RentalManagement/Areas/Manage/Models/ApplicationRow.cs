using TroyWC_RentalManagement.Models;

namespace TroyWC_RentalManagement.Areas.Manage.Models;

/// <summary>Grid row for the Applications page. Init-only properties so EF can sort on them after projection.</summary>
public class ApplicationRow
{
    public int Id { get; init; }
    public string ApplicantName { get; init; } = string.Empty;
    public string PropertyName { get; init; } = string.Empty;
    public string UnitNumber { get; init; } = string.Empty;
    public AppStatus Status { get; init; }
    public DateTimeOffset? Submitted { get; init; }
}
