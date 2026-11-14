using System.Security.Claims;
using CampusBook.Facade.Interface;
using CampusBook.Framework;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusBook.Web.UI.Controllers;

[Authorize]
public class MessagesController : Controller
{
    private readonly IConversationFacade _conversationFacade;

    public MessagesController(IConversationFacade conversationFacade)
    {
        _conversationFacade = conversationFacade;
    }

    private int GetCurrentUserId()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(idClaim, out var id) ? id : 0;
    }

    [HttpGet]
    [Route(UrlRewrite.Messages.Index)]
    public async Task<IActionResult> Index()
    {
        var userId = GetCurrentUserId();
        var conversations = await _conversationFacade.GetUserConversationsAsync(userId);
        return View(conversations);
    }

    [HttpGet]
    [Route(UrlRewrite.Messages.Conversation)]
    public async Task<IActionResult> Conversation(int id)
    {
        var userId = GetCurrentUserId();
        var model = await _conversationFacade.GetConversationDetailsAsync(id, userId);
        if (model == null)
        {
            TempData["ErrorMessage"] = "Conversation not found or unauthorized.";
            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }

    [HttpPost]
    [Route(UrlRewrite.Messages.Send)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(int conversationId, string messageContent)
    {
        var userId = GetCurrentUserId();
        var result = await _conversationFacade.SendMessageAsync(conversationId, userId, messageContent);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
        }

        return RedirectToAction(nameof(Conversation), new { id = conversationId });
    }
}
