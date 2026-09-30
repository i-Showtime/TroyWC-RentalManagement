namespace TroyWC_RentalManagement.Models;

public class Lease 
{
    public int Id { get; set; }

    public int UnitId { get; set; }

    public int ApplicationId { get; set; }

    public LeaseStatus Status { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public DateTimeOffset Created { get; set; }

    public Unit Unit { get; set; } = null!;

    public RentalApplication Application { get; set; } = null!;
}



