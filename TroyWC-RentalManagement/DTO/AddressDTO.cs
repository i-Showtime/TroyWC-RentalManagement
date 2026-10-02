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

    /// <summary>"Line1, Line2, City, State PostalCode", skipping blank parts.</summary>
    public string ToSingleLine()
    {
        var region = string.Join(" ", new[] { State, PostalCode }.Where(p => !string.IsNullOrWhiteSpace(p)));
        return string.Join(", ", new[] { Line1, Line2, City, region }.Where(p => !string.IsNullOrWhiteSpace(p)));
    }

    /// <summary>
    /// Null-safe so it can also save unvalidated input (model binding turns empty fields into null);
    /// values are cut to the column lengths for the same reason.
    /// </summary>
    public void ApplyTo(Address address)
    {
        address.Line1 = InputText.Clean(Line1, 200) ?? string.Empty;
        address.Line2 = InputText.Clean(Line2, 200);
        address.City = InputText.Clean(City, 100) ?? string.Empty;
        address.State = InputText.Clean(State, 50) ?? string.Empty;
        address.PostalCode = InputText.Clean(PostalCode, 20) ?? string.Empty;
    }
}
