using System.Security.Claims;
using CampusBook.Facade.Interface;
using CampusBook.Facade.ViewModels.Books;
using CampusBook.Framework;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusBook.Web.UI.Controllers;

public class BooksController : Controller
{
    private readonly IBookFacade _bookFacade;
    private readonly IUserFacade _userFacade;
    private readonly ILogger<BooksController> _logger;

    public BooksController(
        IBookFacade bookFacade,
        IUserFacade userFacade,
        ILogger<BooksController> logger)
    {
        _bookFacade = bookFacade;
        _userFacade = userFacade;
        _logger = logger;
    }

    private int? GetCurrentUserId()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(idClaim, out var id) ? id : null;
    }

    [HttpGet]
    [Route(UrlRewrite.Books.Catalogue)]
    [AllowAnonymous]
    public async Task<IActionResult> Index(BookFilterViewModel filter)
    {
        var result = await _bookFacade.GetCatalogueAsync(filter);
        return View(result);
    }

    [HttpGet]
    [Route(UrlRewrite.Books.Details)]
    [AllowAnonymous]
    public async Task<IActionResult> Details(int id)
    {
        var currentUserId = GetCurrentUserId();
        var bookDetails = await _bookFacade.GetBookDetailsAsync(id, currentUserId);
        if (bookDetails == null)
        {
            return NotFound("Book listing not found.");
        }

        return View(bookDetails);
    }

    [HttpGet]
    [Route(UrlRewrite.Books.Create)]
    [Authorize]
    public async Task<IActionResult> Create()
    {
        var userId = GetCurrentUserId()!.Value;
        if (!await _userFacade.IsEmailConfirmedAsync(userId))
        {
            TempData["ErrorMessage"] = "You must verify your university email before you can publish book listings.";
            return RedirectToAction(nameof(Index));
        }

        var model = new BookCreateViewModel
        {
            Categories = await _bookFacade.GetCategoriesDropdownAsync(),
            Departments = await _bookFacade.GetDepartmentsDropdownAsync()
        };

        return View(model);
    }

    [HttpPost]
    [Route(UrlRewrite.Books.Create)]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BookCreateViewModel model)
    {
        var userId = GetCurrentUserId()!.Value;
        if (!await _userFacade.IsEmailConfirmedAsync(userId))
        {
            TempData["ErrorMessage"] = "You must verify your university email before you can publish book listings.";
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
        {
            model.Categories = await _bookFacade.GetCategoriesDropdownAsync(model.CategoryId);
            model.Departments = await _bookFacade.GetDepartmentsDropdownAsync(model.DepartmentId);
            return View(model);
        }

        var result = await _bookFacade.CreateBookAsync(userId, model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Could not publish book listing.");
            model.Categories = await _bookFacade.GetCategoriesDropdownAsync(model.CategoryId);
            model.Departments = await _bookFacade.GetDepartmentsDropdownAsync(model.DepartmentId);
            return View(model);
        }

        TempData["SuccessMessage"] = "Your book listing has been published successfully!";
        return RedirectToAction(nameof(Details), new { id = result.Data });
    }

    [HttpGet]
    [Route(UrlRewrite.Books.MyBooks)]
    [Authorize]
    public async Task<IActionResult> MyBooks()
    {
        var userId = GetCurrentUserId()!.Value;
        var myBooks = await _bookFacade.GetUserBooksAsync(userId);
        return View(myBooks);
    }

    [HttpGet]
    [Route(UrlRewrite.Books.Edit)]
    [Authorize]
    public async Task<IActionResult> Edit(int id)
    {
        var userId = GetCurrentUserId()!.Value;
        var editModel = await _bookFacade.GetBookForEditAsync(id, userId);
        if (editModel == null)
        {
            TempData["ErrorMessage"] = "Book listing not found or you are not authorized to edit it.";
            return RedirectToAction(nameof(MyBooks));
        }

        return View(editModel);
    }

    [HttpPost]
    [Route(UrlRewrite.Books.Edit)]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(BookEditViewModel model)
    {
        var userId = GetCurrentUserId()!.Value;
        if (!ModelState.IsValid)
        {
            model.Categories = await _bookFacade.GetCategoriesDropdownAsync(model.CategoryId);
            model.Departments = await _bookFacade.GetDepartmentsDropdownAsync(model.DepartmentId);
            return View(model);
        }

        var result = await _bookFacade.UpdateBookAsync(userId, model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Could not update book listing.");
            model.Categories = await _bookFacade.GetCategoriesDropdownAsync(model.CategoryId);
            model.Departments = await _bookFacade.GetDepartmentsDropdownAsync(model.DepartmentId);
            return View(model);
        }

        TempData["SuccessMessage"] = "Book listing updated successfully.";
        return RedirectToAction(nameof(MyBooks));
    }

    [HttpPost]
    [Route(UrlRewrite.Books.Delete)]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetCurrentUserId()!.Value;
        var result = await _bookFacade.DeactivateBookAsync(id, userId);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.ErrorMessage ?? "Could not deactivate listing.";
        }
        else
        {
            TempData["SuccessMessage"] = "Book listing has been deactivated.";
        }

        return RedirectToAction(nameof(MyBooks));
    }
}

