using Microsoft.AspNetCore.Mvc;
using TroyWC_RentalManagement.Areas.Applicant.Models;

namespace TroyWC_RentalManagement.Areas.Applicant.ViewComponents;

/// <summary>Step 1 of the rental application: the applicant's basic information. Renders Areas/Applicant/Views/Shared/Components/ApplicantInfo/Default.cshtml.</summary>
public class ApplicantInfoViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(RentalApplicationViewModel model) => View(model);
}
