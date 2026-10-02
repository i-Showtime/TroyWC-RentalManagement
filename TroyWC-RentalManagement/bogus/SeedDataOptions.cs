namespace TroyWC_RentalManagement.bogus;

/// <summary>The "SeedData" section of appsettings.Development.json, read by <see cref="DevDataSeeder"/>.</summary>
public sealed class SeedDataOptions
{
    public const string SectionName = "SeedData";

    public bool Enabled { get; set; }

    /// <summary>Logins to create. appsettings is the source of truth: a changed password is applied on the next run.</summary>
    public List<SeedUser> Users { get; set; } = [];
}

public sealed class SeedUser
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    /// <summary>A <see cref="Models.Roles"/> value.</summary>
    public string Role { get; set; } = string.Empty;
}
