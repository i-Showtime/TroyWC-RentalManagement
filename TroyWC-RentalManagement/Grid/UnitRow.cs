namespace TroyWC_RentalManagement.Grid;

/// <summary>Grid row for unit lists. Init-only properties so EF can sort on them after projection.</summary>
public class UnitRow
{
    public int Id { get; init; }
    public string PropertyName { get; init; } = string.Empty;
    public string UnitNumber { get; init; } = string.Empty;
    public int Bedrooms { get; init; }
    public decimal RentAmount { get; init; }

    /// <summary>True while the unit has an active lease.</summary>
    public bool IsLeased { get; init; }
}
