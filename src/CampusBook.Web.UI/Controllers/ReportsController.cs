using System.Security.Claims;
using CampusBook.Entities;
using CampusBook.Facade.Interface;
using CampusBook.Facade.ViewModels.Admin;
using CampusBook.Framework;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusBook.Web.UI.Controllers;

[Authorize]
public class ReportsController : Controller
{
    private readonly IAdminFacade _adminFacade;

    public ReportsController(IAdminFacade adminFacade)
    {
        _adminFacade = adminFacade;
    }

    private int GetCurrentUserId()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(idClaim, out var id) ? id : 0;
    }

    [HttpGet]
    [Route(UrlRewrite.Reports.Create)]
    public IActionResult Create(ReportTargetType targetType, int targetId, string? targetTitle = null, string? returnUrl = null)
    {
        var model = new CreateReportViewModel
        {
            TargetType = targetType,
            TargetId = targetId,
            TargetTitle = targetTitle ?? $"Item #{targetId}",
            ReturnUrl = returnUrl
        };

        return View(model);
    }

    [HttpPost]
    [Route(UrlRewrite.Reports.Create)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateReportViewModel model)
    {
        var userId = GetCurrentUserId();
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _adminFacade.SubmitReportAsync(userId, model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Could not submit report.");
            return View(model);
        }

        TempData["SuccessMessage"] = "Your report has been submitted for moderation review. Thank you for keeping CampusBook safe.";

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return RedirectToAction("Index", "Home");
    }
}
