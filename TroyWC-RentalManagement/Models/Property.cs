namespace TroyWC_RentalManagement.Models;

public class Property
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public Address Address { get; set; } = new();

    public DateTimeOffset CreatedAt { get; set; }

    public bool IsDeleted { get; set; }
}

