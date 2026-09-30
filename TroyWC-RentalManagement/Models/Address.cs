using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TroyWC_RentalManagement.Models;

[ComplexType]
public class Address
{
    [MaxLength(200)]
    public string Line1 { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Line2 { get; set; }

    [MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [MaxLength(50)]
    public string State { get; set; } = string.Empty;

    [MaxLength(20)]
    public string PostalCode { get; set; } = string.Empty;

}