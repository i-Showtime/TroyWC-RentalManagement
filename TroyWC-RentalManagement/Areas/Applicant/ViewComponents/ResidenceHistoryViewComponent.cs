using Microsoft.AspNetCore.Mvc;
using TroyWC_RentalManagement.Areas.Applicant.Models;

namespace TroyWC_RentalManagement.Areas.Applicant.ViewComponents;

/// <summary>Step 2 of the rental application: prior residences, edited in a modal. Renders Areas/Applicant/Views/Shared/Components/ResidenceHistory/Default.cshtml.</summary>
public class ResidenceHistoryViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(RentalApplicationViewModel model) => View(model);
}
