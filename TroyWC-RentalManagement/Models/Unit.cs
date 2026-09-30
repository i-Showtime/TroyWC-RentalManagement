namespace TroyWC_RentalManagement.Models;

public class Unit
{
    public int Id { get; set; }

    public int PropertyId { get; set; }

    public string UnitNumnber { get; set; } = string.Empty;

    public int Bedrooms { get; set; }

    public int Bathrooms { get; set; }

    public int? SquareFeet { get; set; }

    public int Rent { get; set; }

    public LeaseStatus Status { get; set; }

    public bool IsDeleted { get; set; }
}



