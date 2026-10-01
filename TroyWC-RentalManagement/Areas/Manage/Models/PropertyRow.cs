namespace TroyWC_RentalManagement.Areas.Manage.Models;

/// <summary>Grid row for the Properties page. Init-only properties so EF can sort on them after projection.</summary>
public class PropertyRow
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Line1 { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    public int UnitCount { get; init; }
}
