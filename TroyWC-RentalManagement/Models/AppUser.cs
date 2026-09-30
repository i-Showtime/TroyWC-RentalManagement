using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace TroyWC_RentalManagement.Models;

public static class Roles
{
    public const string Applicant = "Applicant"; 
    public const string PropertyManager = "PropertyManager"; 
}

public class AppUser : IdentityUser<Guid>
{
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

}

