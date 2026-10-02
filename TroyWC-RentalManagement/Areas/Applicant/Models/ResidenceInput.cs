using System.ComponentModel.DataAnnotations;
using TroyWC_RentalManagement.DTO;
using TroyWC_RentalManagement.Models;

namespace TroyWC_RentalManagement.Areas.Applicant.Models;

/// <summary>
/// Section 2: one prior residence. Dates stay raw text (like <see cref="Residence"/>) so the
/// Save button can keep whatever was entered.
/// </summary>
public class ResidenceInput : IValidatableObject
{
    public AddressDTO Address { get; set; } = new();

    [Required, MaxLength(200)]
    [Display(Name = "Landlord name")]
    public string? LandlordName { get; set; }

    [Required, Phone, MaxLength(50)]
    [Display(Name = "Landlord phone")]
    public string? LandlordPhone { get; set; }

    [Required, PastIsoDate]
    [Display(Name = "Move-in date")]
    public string? MoveInDate { get; set; }

    [Required, PastIsoDate]
    [Display(Name = "Move-out date")]
    public string? MoveOutDate { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (IsoDate.TryParse(MoveInDate, out var moveIn)
            && IsoDate.TryParse(MoveOutDate, out var moveOut)
            && moveOut < moveIn)
        {
            yield return new ValidationResult(
                "Move-out date can't be before the move-in date.", [nameof(MoveOutDate)]);
        }
    }

    public static ResidenceInput FromEntity(Residence residence) => new()
    {
        Address = AddressDTO.FromAddress(residence.Address),
        LandlordName = residence.LandlordName,
        LandlordPhone = residence.LandlordPhone,
        MoveInDate = residence.MoveInDate,
        MoveOutDate = residence.MoveOutDate,
    };

    /// <summary>Safe to call with unvalidated input (the Save button).</summary>
    public void ApplyTo(Residence residence)
    {
        Address.ApplyTo(residence.Address);
        residence.LandlordName = InputText.Clean(LandlordName, 200);
        residence.LandlordPhone = InputText.Clean(LandlordPhone, 50);
        residence.MoveInDate = InputText.Clean(MoveInDate, 50);
        residence.MoveOutDate = InputText.Clean(MoveOutDate, 50);

        // The current address lives on ApplicationApplicant; these are all prior residences.
        residence.IsCurrent = false;
    }
}
