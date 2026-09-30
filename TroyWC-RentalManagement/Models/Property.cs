using System.ComponentModel.DataAnnotations;

namespace TroyWC_RentalManagement.Models;

public class Property 
{

    public int Id { get; set; }
    
    [Required, MaxLength(200)]
    public string Name { get; set; } = null!;

    public Address Address { get; set; } = new();

    public bool IsDeleted { get; set; }

    public DateTimeOffset Created { get; set; }

    public DateTimeOffset? Updated { get; set; }

    public ICollection<Unit> Units { get; set; } = new List<Unit>();
}

