using System.Security.Claims;
using CampusBook.Facade.Interface;
using CampusBook.Facade.ViewModels.History;
using CampusBook.Framework;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusBook.Web.UI.Controllers;

[Authorize]
public class HistoryController : Controller
{
    private readonly IExchangeHistoryFacade _historyFacade;

    public HistoryController(IExchangeHistoryFacade historyFacade)
    {
        _historyFacade = historyFacade;
    }

    private int GetCurrentUserId()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(idClaim, out var id) ? id : 0;
    }

    [HttpGet]
    [Route(UrlRewrite.History.Index)]
    public async Task<IActionResult> Index(HistoryRoleFilter filter = HistoryRoleFilter.All)
    {
        var userId = GetCurrentUserId();
        var model = await _historyFacade.GetUserExchangeHistoryAsync(userId, filter);
        return View(model);
    }
}
