using System.Security.Claims;
using CampusBook.Facade.Interface;
using CampusBook.Framework;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusBook.Web.UI.Controllers;

[Authorize]
public class WishlistController : Controller
{
    private readonly IWishlistFacade _wishlistFacade;
    private readonly ILogger<WishlistController> _logger;

    public WishlistController(
        IWishlistFacade wishlistFacade,
        ILogger<WishlistController> logger)
    {
        _wishlistFacade = wishlistFacade;
        _logger = logger;
    }

    private int GetCurrentUserId()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(idClaim, out var id) ? id : 0;
    }

    [HttpGet]
    [Route(UrlRewrite.Wishlist.Index)]
    public async Task<IActionResult> Index()
    {
        var userId = GetCurrentUserId();
        var model = await _wishlistFacade.GetUserWishlistAsync(userId);
        return View(model);
    }

    [HttpPost]
    [Route(UrlRewrite.Wishlist.Toggle)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int bookId, string? returnUrl = null)
    {
        var userId = GetCurrentUserId();
        var result = await _wishlistFacade.ToggleWishlistAsync(userId, bookId);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
        }
        else
        {
            TempData["SuccessMessage"] = result.Data
                ? "Book added to your wishlist! 💖"
                : "Book removed from your wishlist.";
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Details", "Books", new { id = bookId });
    }

    [HttpPost]
    [Route(UrlRewrite.Wishlist.Remove)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int bookId)
    {
        var userId = GetCurrentUserId();
        var result = await _wishlistFacade.RemoveFromWishlistAsync(userId, bookId);

        if (result.Success)
        {
            TempData["SuccessMessage"] = "Book removed from your wishlist.";
        }
        else
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
        }

        return RedirectToAction(nameof(Index));
    }
}
