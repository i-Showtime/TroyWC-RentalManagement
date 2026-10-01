using System.ComponentModel.DataAnnotations;
using TroyWC_RentalManagement.DTO;
using TroyWC_RentalManagement.Models;

namespace TroyWC_RentalManagement.Areas.Manage.Models;

public class PropertyFormViewModel
{
    public int? Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public AddressDTO Address { get; set; } = new();

    public static PropertyFormViewModel FromEntity(Property property) => new()
    {
        Id = property.Id,
        Name = property.Name,
        Address = AddressDTO.FromAddress(property.Address),
    };

    public void ApplyTo(Property property)
    {
        property.Name = Name.Trim();
        Address.ApplyTo(property.Address);
    }
}
