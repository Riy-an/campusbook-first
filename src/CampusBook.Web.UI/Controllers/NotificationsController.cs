using System.Security.Claims;
using CampusBook.Facade.Interface;
using CampusBook.Framework;
using CampusBook.Web.UI.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusBook.Web.UI.Controllers;

[Authorize]
public class NotificationsController : Controller
{
    private readonly INotificationFacade _notificationFacade;

    public NotificationsController(INotificationFacade notificationFacade)
    {
        _notificationFacade = notificationFacade;
    }

    private int GetCurrentUserId()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(idClaim, out var id) ? id : 0;
    }

    [HttpGet]
    [Route(UrlRewrite.Notifications.Index)]
    [HasPermission(Permissions.Notifications.ViewOwn)]
    public async Task<IActionResult> Index(int page = 1)
    {
        var userId = GetCurrentUserId();
        var model = await _notificationFacade.GetUserNotificationsAsync(userId, page, 20);
        return View(model);
    }

    [HttpPost]
    [Route(UrlRewrite.Notifications.MarkAsRead)]
    [HasPermission(Permissions.Notifications.ManageOwn)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsRead(int id, string? returnUrl = null)
    {
        var userId = GetCurrentUserId();
        await _notificationFacade.MarkAsReadAsync(id, userId);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Route(UrlRewrite.Notifications.MarkAllAsRead)]
    [HasPermission(Permissions.Notifications.ManageOwn)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetCurrentUserId();
        await _notificationFacade.MarkAllAsReadAsync(userId);
        TempData["SuccessMessage"] = "All notifications marked as read.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Route("notifications/unread-count")]
    [HasPermission(Permissions.Notifications.ViewOwn)]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = GetCurrentUserId();
        var count = await _notificationFacade.GetUnreadCountAsync(userId);
        return Json(new { count });
    }
}
