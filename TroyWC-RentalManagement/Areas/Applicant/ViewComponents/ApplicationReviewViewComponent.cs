using Microsoft.AspNetCore.Mvc;
using TroyWC_RentalManagement.Areas.Applicant.Models;

namespace TroyWC_RentalManagement.Areas.Applicant.ViewComponents;

/// <summary>Step 3 of the rental application: read-only summary of steps 1 and 2, with Submit. Renders Areas/Applicant/Views/Shared/Components/ApplicationReview/Default.cshtml.</summary>
public class ApplicationReviewViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(RentalApplicationViewModel model) => View(model);
}
