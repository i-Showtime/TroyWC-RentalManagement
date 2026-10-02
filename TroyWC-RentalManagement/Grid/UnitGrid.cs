using System.Linq.Expressions;
using TroyWC_RentalManagement.Models;

namespace TroyWC_RentalManagement.Grid;

/// <summary>Projection and columns shared by every unit grid; each page adds its own actions.</summary>
public static class UnitGrid
{
    public static readonly Expression<Func<Unit, UnitRow>> ToRow = u => new UnitRow
    {
        Id = u.Id,
        PropertyName = u.Property.Name,
        UnitNumber = u.UnitNumber,
        Bedrooms = u.Bedrooms,
        RentAmount = u.RentAmount,
    };

    public static IReadOnlyList<GridColumn<UnitRow>> Columns { get; } =
    [
        GridColumn<UnitRow>.For(r => r.PropertyName, "Property"),
        GridColumn<UnitRow>.For(r => r.UnitNumber, "Unit #", sortKey: "unit"),
        GridColumn<UnitRow>.For(r => r.Bedrooms, "Bedrooms", display: FormatBedrooms),
        GridColumn<UnitRow>.For(r => r.RentAmount, "Rent", format: "C"),
    ];

    public const string DefaultSort = "property";

    public static IReadOnlyList<string> ThenBy { get; } = ["unit"];

    public static string FormatBedrooms(int bedrooms) => bedrooms == 0 ? "Studio" : bedrooms.ToString();
}
