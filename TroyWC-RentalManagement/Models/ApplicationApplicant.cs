using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TroyWC_RentalManagement.Models;

public class ApplicationApplicant 
{
    public int Id { get; set; }

    public int ApplicationId { get; set; }

    public int UserId { get; set; }

    /// <summary>
    /// Denormalized copy of RentalApplication.UnitId.
    /// Maintain this value in application/service logic.
    /// </summary>
    public int UnitId { get; set; }

    public bool IsPrimary { get; set; }

    [Required, MaxLength(100)]
    public string FirstName { get; set; } = null!;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = null!;

    [MaxLength(50)]
    public string? Phone { get; set; }

    [MaxLength(320)]
    [EmailAddress]
    public string? Email { get; set; }

    public Address CurrentAddress { get; set; } = new();

    public DateTime? Withdrawn { get; set; }

    public DateTime? Updated { get; set; }

    public RentalApplication Application { get; set; } = null!;

    public ICollection<Residence> Residences { get; set; } =
        new List<Residence>();
}



