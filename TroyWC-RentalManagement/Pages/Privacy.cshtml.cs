using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TroyWC_RentalManagement.Models;

namespace TroyWC_RentalManagement.Pages
{
    [Authorize(Roles = Roles.Applicant)]
    public class PrivacyModel : PageModel
    {
        public void OnGet()
        {
        }
    }

}
