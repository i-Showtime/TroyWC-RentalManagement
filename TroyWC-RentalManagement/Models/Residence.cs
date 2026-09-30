using System.ComponentModel.DataAnnotations;

namespace TroyWC_RentalManagement.Models;

public class Residence 
{
    public int Id { get; set; }
    public int ApplicationApplicantId { get; set; }

    public Address Address { get; set; } = new();

    [MaxLength(200)]
    public string? LandlordName { get; set; }

    [MaxLength(50)]
    public string? LandlordPhone { get; set; }

    public bool IsCurrent { get; set; }

    /// <summary>
    /// Raw user-entered text; parse and validate outside this entity.
    /// </summary>
    [MaxLength(50)]
    public string? MoveInDate { get; set; }

    /// <summary>
    /// Raw user-entered text; parse and validate outside this entity.
    /// </summary>
    [MaxLength(50)]
    public string? MoveOutDate { get; set; }

    public ApplicationApplicant ApplicationApplicant { get; set; } = null!;
}

