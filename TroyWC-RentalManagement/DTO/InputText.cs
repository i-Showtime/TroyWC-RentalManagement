namespace TroyWC_RentalManagement.DTO;

public static class InputText
{
    /// <summary>
    /// Trims <paramref name="value"/> and cuts it to <paramref name="maxLength"/>; null when blank.
    /// Used when saving unvalidated input so it still fits the column.
    /// </summary>
    public static string? Clean(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
