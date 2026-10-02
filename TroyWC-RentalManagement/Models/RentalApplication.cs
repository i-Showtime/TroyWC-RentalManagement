using System.ComponentModel.DataAnnotations;

namespace TroyWC_RentalManagement.Models;

public class RentalApplication 
{
    public int Id { get; set; }
    public int UnitId { get; set; }

    /// <summary>Stored as the enum name (see ApplicationDbContext).</summary>
    public AppStatus Status { get; set; }

    [MaxLength(450)]
    public string? AssignedManagerId { get; set; }

    /// <summary>Identity user id of the applicant who started the application.</summary>
    [Required, MaxLength(450)]
    public string CreatedByUserId { get; set; } = null!;

    /// <summary>Set when the applicant passes Continue on the applicant info section.</summary>
    public bool ApplicantInfoCompleted { get; set; }

    /// <summary>Set when the applicant passes Continue on the residence history section.</summary>
    public bool ResidenceHistoryCompleted { get; set; }

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

    [Required, MaxLength(450)]
    public string AuthorUserId { get; set; } = null!;

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

    [Required, MaxLength(450)]
    public string ActorUserId { get; set; } = null!;

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

