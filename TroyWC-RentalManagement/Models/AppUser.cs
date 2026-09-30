using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace TroyWC_RentalManagement.Models;

public class AppUser : IdentityUser
{
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

}

