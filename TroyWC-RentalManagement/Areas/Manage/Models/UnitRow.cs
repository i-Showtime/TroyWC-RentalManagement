namespace TroyWC_RentalManagement.Areas.Manage.Models;

/// <summary>Grid row for the Units page. Init-only properties so EF can sort on them after projection.</summary>
public class UnitRow
{
    public int Id { get; init; }
    public string PropertyName { get; init; } = string.Empty;
    public string UnitNumber { get; init; } = string.Empty;
    public int Bedrooms { get; init; }
    public decimal RentAmount { get; init; }
}
