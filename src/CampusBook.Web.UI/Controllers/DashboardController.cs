using System.Security.Claims;
using CampusBook.Facade.Interface;
using CampusBook.Framework;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusBook.Web.UI.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IDashboardFacade _dashboardFacade;

    public DashboardController(IDashboardFacade dashboardFacade)
    {
        _dashboardFacade = dashboardFacade;
    }

    [HttpGet]
    [Route(UrlRewrite.Dashboard.Index)]
    public async Task<IActionResult> Index()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(idClaim, out var userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var dashboardData = await _dashboardFacade.GetDashboardDataAsync(userId);
        return View(dashboardData);
    }
}

