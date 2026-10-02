using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TroyWC_RentalManagement.Areas.Manage.Models;
using TroyWC_RentalManagement.Data;
using TroyWC_RentalManagement.Grid;
using TroyWC_RentalManagement.Models;

namespace TroyWC_RentalManagement.Areas.Manage.Controllers;

[Area("Manage")]
[Route("Manage/[controller]")]
[Authorize(Roles = Roles.PropertyManager)]
[AutoValidateAntiforgeryToken]
public class UnitsController(ApplicationDbContext context) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] GridRequest grid, int? propertyId, CancellationToken ct)
    {
        var query = context.Units
            .AsNoTracking()
            .Where(u => !u.IsDeleted && !u.Property.IsDeleted);

        if (propertyId is not null)
            query = query.Where(u => u.PropertyId == propertyId);

        var routeValues = propertyId is null
            ? null
            : new Dictionary<string, string> { ["propertyId"] = propertyId.Value.ToString() };

        var gridModel = await query
            .Select(UnitGrid.ToRow)
            .ToGridAsync(BuildGrid(), grid, routeValues, ct);

        if (Request.IsAjax())
            return PartialView("_Grid", gridModel);

        return View(new UnitsIndexViewModel
        {
            Grid = gridModel,
            PropertyId = propertyId,
            Properties = await PropertyOptionsAsync(ct),
        });
    }

    [HttpGet("Create")]
    public async Task<IActionResult> Create(int? propertyId, CancellationToken ct)
    {
        var model = new UnitFormViewModel { PropertyId = propertyId };
        return await FormAsync(model, ct);
    }

    [HttpPost("Create")]
    public async Task<IActionResult> Create(UnitFormViewModel model, CancellationToken ct)
    {
        await ValidateAsync(model, ct);
        if (!ModelState.IsValid)
            return await FormAsync(model, ct);

        var unit = new Unit();
        model.ApplyTo(unit);
        context.Units.Add(unit);
        await context.SaveChangesAsync(ct);

        return Json(new { success = true });
    }

    [HttpGet("Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var unit = await context.Units
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, ct);

        return unit is null ? NotFound() : await FormAsync(UnitFormViewModel.FromEntity(unit), ct);
    }

    [HttpPost("Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, UnitFormViewModel model, CancellationToken ct)
    {
        if (model.Id != id)
            return BadRequest();

        var unit = await context.Units.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, ct);
        if (unit is null)
            return NotFound();

        await ValidateAsync(model, ct);
        if (!ModelState.IsValid)
            return await FormAsync(model, ct);

        model.ApplyTo(unit);
        await context.SaveChangesAsync(ct);

        return Json(new { success = true });
    }

    [HttpGet("Delete/{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var model = await BuildDeleteModelAsync(id, ct);
        return model is null ? NotFound() : PartialView("_DeleteConfirm", model);
    }

    [HttpPost("Delete/{id:int}"), ActionName(nameof(Delete))]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken ct)
    {
        var model = await BuildDeleteModelAsync(id, ct);
        if (model is null)
            return NotFound();

        if (model.BlockedReason is not null)
            return PartialView("_DeleteConfirm", model);

        await context.Units
            .Where(u => u.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.IsDeleted, true), ct);

        return Json(new { success = true });
    }

    /// <summary>Rules that need the database: the property must exist and unit numbers are unique per property.</summary>
    private async Task ValidateAsync(UnitFormViewModel model, CancellationToken ct)
    {
        if (model.PropertyId is not int propertyId)
            return;

        if (!await context.Properties.AnyAsync(p => p.Id == propertyId && !p.IsDeleted, ct))
        {
            ModelState.AddModelError(nameof(model.PropertyId), "Select an existing property.");
            return;
        }

        if (string.IsNullOrWhiteSpace(model.UnitNumber))
            return;

        var unitNumber = model.UnitNumber.Trim();
        var duplicate = await context.Units.AnyAsync(u =>
            u.PropertyId == propertyId
            && !u.IsDeleted
            && u.UnitNumber == unitNumber
            && u.Id != model.Id, ct);

        if (duplicate)
            ModelState.AddModelError(nameof(model.UnitNumber), "This property already has a unit with that number.");
    }

    /// <summary>A unit can't be deleted while it has an active lease.</summary>
    private async Task<DeleteViewModel?> BuildDeleteModelAsync(int id, CancellationToken ct)
    {
        var unit = await context.Units
            .AsNoTracking()
            .Where(u => u.Id == id && !u.IsDeleted)
            .Select(u => new
            {
                PropertyName = u.Property.Name,
                u.UnitNumber,
                HasActiveLease = u.Leases.Any(l => l.Status == LeaseStatus.Active),
            })
            .FirstOrDefaultAsync(ct);

        if (unit is null)
            return null;

        var displayName = $"{unit.PropertyName} – unit {unit.UnitNumber}";

        return new DeleteViewModel
        {
            Id = id,
            EntityName = "unit",
            DisplayName = displayName,
            BlockedReason = unit.HasActiveLease
                ? $"{displayName} has an active lease and can't be deleted."
                : null,
        };
    }

    private async Task<IActionResult> FormAsync(UnitFormViewModel model, CancellationToken ct)
    {
        model.Properties = await PropertyOptionsAsync(ct);
        return PartialView("_UnitForm", model);
    }

    private Task<List<SelectListItem>> PropertyOptionsAsync(CancellationToken ct) =>
        context.Properties
            .AsNoTracking()
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.Name)
            .Select(p => new SelectListItem(p.Name, p.Id.ToString()))
            .ToListAsync(ct);

    private GridDefinition<UnitRow> BuildGrid() => new()
    {
        Columns = UnitGrid.Columns,
        Key = r => r.Id,
        DefaultSort = UnitGrid.DefaultSort,
        ThenBy = UnitGrid.ThenBy,
        Actions =
        [
            new("Edit", r => Url.Action(nameof(Edit), new { id = r.Id })!),
            new("Delete", r => Url.Action(nameof(Delete), new { id = r.Id })!, "btn btn-sm btn-outline-danger"),
        ],
        EmptyMessage = "No units found.",
    };
}
