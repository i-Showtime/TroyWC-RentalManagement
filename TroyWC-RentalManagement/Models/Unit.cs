using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace TroyWC_RentalManagement.Models;

public class Unit 
{
    public int Id { get; set; }
    public int PropertyId { get; set; }

    [Required, MaxLength(20)]
    public string UnitNumber { get; set; } = null!;

    [Range(0, 4)]
    public int Bedrooms { get; set; }

    [Precision(10, 2)]
    [Range(typeof(decimal), "0.01", "999999.99")]
    public decimal RentAmount { get; set; }

    public bool IsDeleted { get; set; }

    public Property Property { get; set; } = null!;

    public ICollection<Lease> Leases { get; set; } = new List<Lease>();

    public ICollection<RentalApplication> RentalApplications { get; set; } =
        new List<RentalApplication>();
    
}
