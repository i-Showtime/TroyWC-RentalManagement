using System.ComponentModel.DataAnnotations;

namespace TroyWC_RentalManagement.Models;

public class RentalApplication 
{
    public int Id { get; set; }
    public int UnitId { get; set; }

    /// <summary>
    /// Allowed status values should be defined by the application workflow.
    /// </summary>
    [Required, MaxLength(50)]
    public string Status { get; set; } = null!;

    public int? AssignedManagerId { get; set; }

    public int CreatedByUserId { get; set; }

    public DateTimeOffset Created { get; set; }

    public DateTimeOffset? Updated { get; set; }

    public DateTimeOffset? Submitted { get; set; }

    public DateTimeOffset? Approved { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;

    public Unit Unit { get; set; } = null!;

    public ICollection<ApplicationApplicant> Applicants { get; set; } =
        new List<ApplicationApplicant>();

    public ICollection<ApplicationComment> Comments { get; set; } =
        new List<ApplicationComment>();

    public ICollection<ApplicationHistory> History { get; set; } =
        new List<ApplicationHistory>();

    public ICollection<Lease> Leases { get; set; } = new List<Lease>();
}

public class ApplicationComment 
{
    public int Id { get; set; }

    public int ApplicationId { get; set; }

    public int AuthorUserId { get; set; }

    [Required, MaxLength(4000)]
    public string Body { get; set; } = null!;

    public DateTimeOffset Created { get; set; }

    public RentalApplication Application { get; set; } = null!;
}

public class ApplicationHistory 
{
    public int Id { get; set; }

    public int ApplicationId { get; set; }

    public DateTimeOffset OccurredTime { get; set; }

    public int ActorUserId { get; set; }

    [Required, MaxLength(100)]
    public string ActorRole { get; set; } = null!;

    [Required, MaxLength(100)]
    public string EventType { get; set; } = null!;

    [MaxLength(50)]
    public string? FromStatus { get; set; }

    [MaxLength(50)]
    public string? ToStatus { get; set; }

    public string? DetailsJson { get; set; }

    public RentalApplication Application { get; set; } = null!;
}

