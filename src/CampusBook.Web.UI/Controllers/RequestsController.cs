using System.Security.Claims;
using CampusBook.Facade.Interface;
using CampusBook.Facade.ViewModels.Requests;
using CampusBook.Framework;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusBook.Web.UI.Controllers;

[Authorize]
public class RequestsController : Controller
{
    private readonly IBookRequestFacade _requestFacade;
    private readonly IUserFacade _userFacade;
    private readonly ILogger<RequestsController> _logger;

    public RequestsController(
        IBookRequestFacade requestFacade,
        IUserFacade userFacade,
        ILogger<RequestsController> logger)
    {
        _requestFacade = requestFacade;
        _userFacade = userFacade;
        _logger = logger;
    }

    private int GetCurrentUserId()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(idClaim, out var id) ? id : 0;
    }

    [HttpGet]
    [Route(UrlRewrite.Requests.Sent)]
    public async Task<IActionResult> Sent()
    {
        var userId = GetCurrentUserId();
        var sentRequests = await _requestFacade.GetSentRequestsAsync(userId);
        return View(sentRequests);
    }

    [HttpGet]
    [Route(UrlRewrite.Requests.Received)]
    public async Task<IActionResult> Received()
    {
        var userId = GetCurrentUserId();
        var receivedRequests = await _requestFacade.GetReceivedRequestsAsync(userId);
        return View(receivedRequests);
    }

    [HttpGet]
    [Route(UrlRewrite.Requests.Create)]
    public async Task<IActionResult> Create(int bookId)
    {
        var userId = GetCurrentUserId();

        var viewModel = await _requestFacade.GetCreateRequestViewModelAsync(bookId, userId);
        if (viewModel == null)
        {
            TempData["ErrorMessage"] = "You cannot request this book listing (it may be unavailable or owned by you).";
            return RedirectToAction("Details", "Books", new { id = bookId });
        }

        return View(viewModel);
    }

    [HttpPost]
    [Route(UrlRewrite.Requests.Create)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateRequestViewModel model)
    {
        var userId = GetCurrentUserId();
        if (!ModelState.IsValid)
        {
            var reloadModel = await _requestFacade.GetCreateRequestViewModelAsync(model.BookId, userId);
            if (reloadModel != null)
            {
                model.BookTitle = reloadModel.BookTitle;
                model.BookAuthor = reloadModel.BookAuthor;
                model.ExchangeType = reloadModel.ExchangeType;
                model.Price = reloadModel.Price;
                model.ImageUrl = reloadModel.ImageUrl;
                model.OwnerName = reloadModel.OwnerName;
                model.UserAvailableBooks = reloadModel.UserAvailableBooks;
                model.Categories = reloadModel.Categories;
                model.Departments = reloadModel.Departments;
            }
            return View(model);
        }

        var result = await _requestFacade.CreateRequestAsync(userId, model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Could not submit book request.");
            var reloadModel = await _requestFacade.GetCreateRequestViewModelAsync(model.BookId, userId);
            if (reloadModel != null)
            {
                model.BookTitle = reloadModel.BookTitle;
                model.BookAuthor = reloadModel.BookAuthor;
                model.ExchangeType = reloadModel.ExchangeType;
                model.Price = reloadModel.Price;
                model.ImageUrl = reloadModel.ImageUrl;
                model.OwnerName = reloadModel.OwnerName;
                model.UserAvailableBooks = reloadModel.UserAvailableBooks;
                model.Categories = reloadModel.Categories;
                model.Departments = reloadModel.Departments;
            }
            return View(model);
        }

        TempData["SuccessMessage"] = "Your request has been sent to the book owner!";
        return RedirectToAction(nameof(Sent));
    }

    [HttpGet]
    [Route(UrlRewrite.Requests.Details)]
    public async Task<IActionResult> Details(int id)
    {
        var userId = GetCurrentUserId();
        var details = await _requestFacade.GetRequestDetailsAsync(id, userId);
        if (details == null)
        {
            TempData["ErrorMessage"] = "Request not found or you are not authorized to view it.";
            return RedirectToAction(nameof(Sent));
        }

        return View(details);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Accept(int id)
    {
        var userId = GetCurrentUserId();
        var result = await _requestFacade.AcceptRequestAsync(id, userId);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.ErrorMessage ?? "Could not accept request.";
        }
        else
        {
            TempData["SuccessMessage"] = "Request accepted! The book has been marked as reserved for this exchange.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id)
    {
        var userId = GetCurrentUserId();
        var result = await _requestFacade.RejectRequestAsync(id, userId);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.ErrorMessage ?? "Could not reject request.";
        }
        else
        {
            TempData["SuccessMessage"] = "Request has been rejected.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var userId = GetCurrentUserId();
        var result = await _requestFacade.CancelRequestAsync(id, userId);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.ErrorMessage ?? "Could not cancel request.";
        }
        else
        {
            TempData["SuccessMessage"] = "Your request has been cancelled.";
        }

        return RedirectToAction(nameof(Sent));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmHandover(int id)
    {
        var userId = GetCurrentUserId();
        var result = await _requestFacade.ConfirmHandoverAsync(id, userId);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.ErrorMessage ?? "Could not confirm handover.";
        }
        else
        {
            TempData["SuccessMessage"] = "Handover confirmation recorded! When both parties have confirmed, the exchange is marked completed.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }
}

