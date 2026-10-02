using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace TroyWC_RentalManagement.Areas.Applicant.Models;

/// <summary>Dates kept as raw yyyy-MM-dd text, the format &lt;input type="date"&gt; posts.</summary>
public static class IsoDate
{
    public const string Format = "yyyy-MM-dd";

    public static bool TryParse(string? text, out DateOnly date) =>
        DateOnly.TryParseExact(text?.Trim(), Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    /// <summary>Long date for display, or the raw text when it isn't a valid date.</summary>
    public static string Display(string? text) =>
        TryParse(text, out var date) ? date.ToString("MMM d, yyyy", CultureInfo.CurrentCulture) : text ?? string.Empty;
}

/// <summary>A raw yyyy-MM-dd date that is not in the future. Blank values pass; pair with [Required].</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class PastIsoDateAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string text || string.IsNullOrWhiteSpace(text))
            return ValidationResult.Success;

        var name = validationContext.DisplayName;

        if (!IsoDate.TryParse(text, out var date))
            return new ValidationResult($"Enter a valid {name.ToLowerInvariant()}.");

        return date > DateOnly.FromDateTime(DateTime.Today)
            ? new ValidationResult($"{name} can't be in the future.")
            : ValidationResult.Success;
    }
}
