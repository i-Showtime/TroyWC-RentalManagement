using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TroyWC_RentalManagement.Data;
using TroyWC_RentalManagement.Grid;
using TroyWC_RentalManagement.Models;

namespace TroyWC_RentalManagement.Controllers;

/// <summary>Home page: every unit that can currently be applied for.</summary>
[Route("")]
public class HomeController(ApplicationDbContext context) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] GridRequest grid, CancellationToken ct)
    {
        var model = await context.Units
            .AsNoTracking()
            .Where(UnitQueries.IsAvailable)
            .Select(UnitGrid.ToRow)
            .ToGridAsync(BuildGrid(), grid, cancellationToken: ct);

        return Request.IsAjax() ? PartialView("_Grid", model) : View(model);
    }

    private GridDefinition<UnitRow> BuildGrid() => new()
    {
        Columns = UnitGrid.Columns,
        Key = r => r.Id,
        DefaultSort = UnitGrid.DefaultSort,
        ThenBy = UnitGrid.ThenBy,
        Actions = User.IsInRole(Roles.Applicant)
            ?
            [
                new("Apply", r => Url.Action("Apply", "Applications", new { area = "Applicant", unitId = r.Id })!,
                    "btn btn-sm btn-primary", OpensModal: false),
            ]
            : [],
        EmptyMessage = "No units are available right now. Check back soon.",
    };
}
