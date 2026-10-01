using System.ComponentModel.DataAnnotations;
using TroyWC_RentalManagement.Models;

namespace TroyWC_RentalManagement.DTO;

public class AddressDTO
{
    [Required, MaxLength(200)]
    [Display(Name = "Address line 1")]
    public string Line1 { get; set; } = string.Empty;

    [MaxLength(200)]
    [Display(Name = "Address line 2")]
    public string? Line2 { get; set; }

    [Required, MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string State { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    [Display(Name = "Postal code")]
    public string PostalCode { get; set; } = string.Empty;

    public static AddressDTO FromAddress(Address address) => new()
    {
        Line1 = address.Line1,
        Line2 = address.Line2,
        City = address.City,
        State = address.State,
        PostalCode = address.PostalCode,
    };

    public void ApplyTo(Address address)
    {
        address.Line1 = Line1.Trim();
        address.Line2 = string.IsNullOrWhiteSpace(Line2) ? null : Line2.Trim();
        address.City = City.Trim();
        address.State = State.Trim();
        address.PostalCode = PostalCode.Trim();
    }
}
