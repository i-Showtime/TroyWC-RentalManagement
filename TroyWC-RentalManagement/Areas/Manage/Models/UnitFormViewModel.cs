using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using TroyWC_RentalManagement.Models;

namespace TroyWC_RentalManagement.Areas.Manage.Models;

public class UnitFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Select a property.")]
    [Display(Name = "Property")]
    public int? PropertyId { get; set; }

    [Required, MaxLength(20)]
    [Display(Name = "Unit #")]
    public string UnitNumber { get; set; } = string.Empty;

    [Required, Range(0, 4)]
    public int? Bedrooms { get; set; }

    [Required]
    [Range(typeof(decimal), "0.01", "999999.99")]
    [Display(Name = "Monthly rent")]
    public decimal? RentAmount { get; set; }

    [BindNever, ValidateNever]
    public IEnumerable<SelectListItem> Properties { get; set; } = [];

    public static UnitFormViewModel FromEntity(Unit unit) => new()
    {
        Id = unit.Id,
        PropertyId = unit.PropertyId,
        UnitNumber = unit.UnitNumber,
        Bedrooms = unit.Bedrooms,
        RentAmount = unit.RentAmount,
    };

    /// <summary>Call only after validation passed, so the required values are present.</summary>
    public void ApplyTo(Unit unit)
    {
        unit.PropertyId = PropertyId!.Value;
        unit.UnitNumber = UnitNumber.Trim();
        unit.Bedrooms = Bedrooms!.Value;
        unit.RentAmount = RentAmount!.Value;
    }
}
