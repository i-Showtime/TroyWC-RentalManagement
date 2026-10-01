using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
public class PropertiesController(ApplicationDbContext context) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] GridRequest grid, CancellationToken ct)
    {
        var model = await context.Properties
            .AsNoTracking()
            .Where(p => !p.IsDeleted)
            .Select(p => new PropertyRow
            {
                Id = p.Id,
                Name = p.Name,
                Line1 = p.Address.Line1,
                City = p.Address.City,
                State = p.Address.State,
                PostalCode = p.Address.PostalCode,
                UnitCount = p.Units.Count(u => !u.IsDeleted),
            })
            .ToGridAsync(BuildGrid(), grid, cancellationToken: ct);

        return Request.IsAjax() ? PartialView("_Grid", model) : View(model);
    }

    [HttpGet("Create")]
    public IActionResult Create() => PartialView("_PropertyForm", new PropertyFormViewModel());

    [HttpPost("Create")]
    public async Task<IActionResult> Create(PropertyFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return PartialView("_PropertyForm", model);

        var property = new Property { Created = DateTimeOffset.UtcNow };
        model.ApplyTo(property);
        context.Properties.Add(property);
        await context.SaveChangesAsync(ct);

        return Json(new { success = true });
    }

    [HttpGet("Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var property = await context.Properties
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);

        return property is null
            ? NotFound()
            : PartialView("_PropertyForm", PropertyFormViewModel.FromEntity(property));
    }

    [HttpPost("Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, PropertyFormViewModel model, CancellationToken ct)
    {
        if (model.Id != id)
            return BadRequest();

        var property = await context.Properties.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);
        if (property is null)
            return NotFound();

        if (!ModelState.IsValid)
            return PartialView("_PropertyForm", model);

        model.ApplyTo(property);
        property.Updated = DateTimeOffset.UtcNow;
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

        await context.Properties
            .Where(p => p.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.IsDeleted, true)
                .SetProperty(p => p.Updated, DateTimeOffset.UtcNow), ct);

        return Json(new { success = true });
    }

    /// <summary>A property can only be deleted once all of its units are deleted.</summary>
    private async Task<DeleteViewModel?> BuildDeleteModelAsync(int id, CancellationToken ct)
    {
        var property = await context.Properties
            .AsNoTracking()
            .Where(p => p.Id == id && !p.IsDeleted)
            .Select(p => new { p.Name, UnitCount = p.Units.Count(u => !u.IsDeleted) })
            .FirstOrDefaultAsync(ct);

        if (property is null)
            return null;

        return new DeleteViewModel
        {
            Id = id,
            EntityName = "property",
            DisplayName = property.Name,
            BlockedReason = property.UnitCount > 0
                ? $"{property.Name} still has {property.UnitCount} unit(s). Delete its units before deleting the property."
                : null,
        };
    }

    private GridDefinition<PropertyRow> BuildGrid() => new()
    {
        Columns =
        [
            GridColumn<PropertyRow>.For(r => r.Name, "Name"),
            GridColumn<PropertyRow>.For(r => r.Line1, "Address"),
            GridColumn<PropertyRow>.For(r => r.City, "City"),
            GridColumn<PropertyRow>.For(r => r.State, "State"),
            GridColumn<PropertyRow>.For(r => r.PostalCode, "Postal code", sortable: false),
            GridColumn<PropertyRow>.For(r => r.UnitCount, "Units"),
        ],
        Key = r => r.Id,
        DefaultSort = "name",
        Actions =
        [
            new("Units", r => Url.Action("Index", "Units", new { propertyId = r.Id })!,
                "btn btn-sm btn-outline-primary", OpensModal: false),
            new("Edit", r => Url.Action(nameof(Edit), new { id = r.Id })!),
            new("Delete", r => Url.Action(nameof(Delete), new { id = r.Id })!, "btn btn-sm btn-outline-danger"),
        ],
        EmptyMessage = "No properties yet. Use “New property” to add one.",
    };
}
