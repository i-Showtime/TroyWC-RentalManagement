using Microsoft.AspNetCore.Mvc.Rendering;
using TroyWC_RentalManagement.Grid;

namespace TroyWC_RentalManagement.Areas.Manage.Models;

public class UnitsIndexViewModel
{
    public required GridViewModel Grid { get; init; }

    /// <summary>Current property filter; null shows units of every property.</summary>
    public int? PropertyId { get; init; }

    public required IEnumerable<SelectListItem> Properties { get; init; }
}
