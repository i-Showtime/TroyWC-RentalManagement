using Microsoft.AspNetCore.Mvc.Rendering;
using TroyWC_RentalManagement.Grid;
using TroyWC_RentalManagement.Models;

namespace TroyWC_RentalManagement.Areas.Manage.Models;

public class ApplicationsIndexViewModel
{
    public required GridViewModel Grid { get; init; }

    /// <summary>Current status filter; null shows every submitted application.</summary>
    public AppStatus? Status { get; init; }

    public required IEnumerable<SelectListItem> Statuses { get; init; }
}
