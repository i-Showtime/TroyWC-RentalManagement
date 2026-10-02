using System.ComponentModel.DataAnnotations;
using TroyWC_RentalManagement.DTO;
using TroyWC_RentalManagement.Models;

namespace TroyWC_RentalManagement.Areas.Applicant.Models;

/// <summary>Section 1: the applicant's basic information.</summary>
public class ApplicantInfoSection
{
    [Required, MaxLength(100)]
    [Display(Name = "First name")]
    public string? FirstName { get; set; }

    [Required, MaxLength(100)]
    [Display(Name = "Last name")]
    public string? LastName { get; set; }

    [Required, Phone, MaxLength(50)]
    public string? Phone { get; set; }

    [Required, EmailAddress, MaxLength(320)]
    public string? Email { get; set; }

    [Display(Name = "Current address")]
    public AddressDTO CurrentAddress { get; set; } = new();

    public static ApplicantInfoSection FromEntity(ApplicationApplicant applicant) => new()
    {
        FirstName = applicant.FirstName,
        LastName = applicant.LastName,
        Phone = applicant.Phone,
        Email = applicant.Email,
        CurrentAddress = AddressDTO.FromAddress(applicant.CurrentAddress),
    };

    /// <summary>Safe to call with unvalidated input (the Save button).</summary>
    public void ApplyTo(ApplicationApplicant applicant)
    {
        applicant.FirstName = InputText.Clean(FirstName, 100) ?? string.Empty;
        applicant.LastName = InputText.Clean(LastName, 100) ?? string.Empty;
        applicant.Phone = InputText.Clean(Phone, 50);
        applicant.Email = InputText.Clean(Email, 320);
        CurrentAddress.ApplyTo(applicant.CurrentAddress);
    }
}
