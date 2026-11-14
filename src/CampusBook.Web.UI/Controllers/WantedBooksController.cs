using System.Security.Claims;
using CampusBook.Facade.Interface;
using CampusBook.Facade.ViewModels.WantedBooks;
using CampusBook.Framework;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusBook.Web.UI.Controllers;

[Authorize]
public class WantedBooksController : Controller
{
    private readonly IWantedBookFacade _wantedBookFacade;
    private readonly ILogger<WantedBooksController> _logger;

    public WantedBooksController(
        IWantedBookFacade wantedBookFacade,
        ILogger<WantedBooksController> logger)
    {
        _wantedBookFacade = wantedBookFacade;
        _logger = logger;
    }

    private int GetCurrentUserId()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(idClaim, out var id) ? id : 0;
    }

    [HttpGet]
    [Route(UrlRewrite.WantedBooks.Index)]
    [AllowAnonymous]
    public async Task<IActionResult> Index(string? searchTerm, int? categoryId, int? departmentId)
    {
        var model = await _wantedBookFacade.GetActiveWantedBooksAsync(searchTerm, categoryId, departmentId);
        return View(model);
    }

    [HttpGet]
    [Route(UrlRewrite.WantedBooks.MyPosts)]
    public async Task<IActionResult> MyPosts()
    {
        var userId = GetCurrentUserId();
        var posts = await _wantedBookFacade.GetMyWantedBooksAsync(userId);
        return View(posts);
    }

    [HttpGet]
    [Route(UrlRewrite.WantedBooks.Create)]
    public async Task<IActionResult> Create()
    {
        var model = await _wantedBookFacade.GetCreateViewModelAsync();
        return View(model);
    }

    [HttpPost]
    [Route(UrlRewrite.WantedBooks.Create)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(WantedBookCreateViewModel model)
    {
        var userId = GetCurrentUserId();

        if (!ModelState.IsValid)
        {
            var reloadModel = await _wantedBookFacade.GetCreateViewModelAsync();
            model.Categories = reloadModel.Categories;
            model.Departments = reloadModel.Departments;
            return View(model);
        }

        var result = await _wantedBookFacade.CreateWantedBookAsync(userId, model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Could not create wanted book post.");
            var reloadModel = await _wantedBookFacade.GetCreateViewModelAsync();
            model.Categories = reloadModel.Categories;
            model.Departments = reloadModel.Departments;
            return View(model);
        }

        TempData["SuccessMessage"] = "Wanted-book request posted! Fellow students can now see your request.";
        return RedirectToAction(nameof(MyPosts));
    }

    [HttpGet]
    [Route(UrlRewrite.WantedBooks.Edit)]
    public async Task<IActionResult> Edit(int id)
    {
        var userId = GetCurrentUserId();
        var model = await _wantedBookFacade.GetEditViewModelAsync(id, userId);
        if (model == null)
        {
            return NotFound("Wanted book post not found or unauthorized.");
        }

        return View(model);
    }

    [HttpPost]
    [Route(UrlRewrite.WantedBooks.Edit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(WantedBookEditViewModel model)
    {
        var userId = GetCurrentUserId();

        if (!ModelState.IsValid)
        {
            var reloadModel = await _wantedBookFacade.GetEditViewModelAsync(model.WantedBookId, userId);
            if (reloadModel != null)
            {
                model.Categories = reloadModel.Categories;
                model.Departments = reloadModel.Departments;
            }
            return View(model);
        }

        var result = await _wantedBookFacade.UpdateWantedBookAsync(userId, model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Could not update wanted book post.");
            var reloadModel = await _wantedBookFacade.GetEditViewModelAsync(model.WantedBookId, userId);
            if (reloadModel != null)
            {
                model.Categories = reloadModel.Categories;
                model.Departments = reloadModel.Departments;
            }
            return View(model);
        }

        TempData["SuccessMessage"] = "Wanted book post updated successfully.";
        return RedirectToAction(nameof(MyPosts));
    }

    [HttpPost]
    [Route(UrlRewrite.WantedBooks.Close)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(int id)
    {
        var userId = GetCurrentUserId();
        var result = await _wantedBookFacade.CloseWantedBookAsync(userId, id);

        if (result.Success)
        {
            TempData["SuccessMessage"] = "Wanted-book post closed.";
        }
        else
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
        }

        return RedirectToAction(nameof(MyPosts));
    }

    [HttpGet]
    [Route(UrlRewrite.WantedBooks.Details)]
    [AllowAnonymous]
    public async Task<IActionResult> Details(int id)
    {
        var currentUserId = GetCurrentUserId();
        var model = await _wantedBookFacade.GetWantedBookDetailsAsync(id, currentUserId);
        if (model == null)
        {
            return NotFound("Wanted book post not found.");
        }

        return View(model);
    }
}
