using System.Security.Claims;
using CampusBook.Facade.Interface;
using CampusBook.Facade.ViewModels.Profile;
using CampusBook.Framework;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusBook.Web.UI.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly IStudentProfileFacade _profileFacade;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(IStudentProfileFacade profileFacade, ILogger<ProfileController> logger)
    {
        _profileFacade = profileFacade;
        _logger = logger;
    }

    private int GetCurrentUserId()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(idClaim, out var id) ? id : 0;
    }

    [HttpGet]
    [Route(UrlRewrite.Profile.MyProfile)]
    public async Task<IActionResult> Index()
    {
        var userId = GetCurrentUserId();
        var profile = await _profileFacade.GetMyProfileAsync(userId);
        if (profile == null)
        {
            return NotFound("Student profile could not be found.");
        }

        return View(profile);
    }

    [HttpGet]
    [Route(UrlRewrite.Profile.EditProfile)]
    public async Task<IActionResult> Edit()
    {
        var userId = GetCurrentUserId();
        var editModel = await _profileFacade.GetProfileForEditAsync(userId);
        if (editModel == null)
        {
            return NotFound("Student profile could not be found.");
        }

        return View(editModel);
    }

    [HttpPost]
    [Route(UrlRewrite.Profile.EditProfile)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProfileEditViewModel model)
    {
        var userId = GetCurrentUserId();
        if (!ModelState.IsValid)
        {
            // Repopulate read-only display fields
            var current = await _profileFacade.GetProfileForEditAsync(userId);
            if (current != null)
            {
                model.StudentId = current.StudentId;
                model.Email = current.Email;
                model.UniversityEmail = current.UniversityEmail;
                model.CurrentProfilePictureUrl = current.CurrentProfilePictureUrl;
                model.CurrentStudentIdCardUrl = current.CurrentStudentIdCardUrl;
                model.IdCardChangeCount = current.IdCardChangeCount;
            }
            return View(model);
        }

        var result = await _profileFacade.UpdateProfileAsync(userId, model);
        if (!result.Success)
        {
            var current = await _profileFacade.GetProfileForEditAsync(userId);
            if (current != null)
            {
                model.StudentId = current.StudentId;
                model.Email = current.Email;
                model.UniversityEmail = current.UniversityEmail;
                model.CurrentProfilePictureUrl = current.CurrentProfilePictureUrl;
                model.CurrentStudentIdCardUrl = current.CurrentStudentIdCardUrl;
                model.IdCardChangeCount = current.IdCardChangeCount;
            }
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Could not update profile.");
            return View(model);
        }

        TempData["SuccessMessage"] = "Profile updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Route(UrlRewrite.Profile.PublicProfile)]
    [AllowAnonymous]
    public async Task<IActionResult> View(int id)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId > 0 && currentUserId == id)
        {
            return RedirectToAction(nameof(Index));
        }

        var profile = await _profileFacade.GetPublicProfileAsync(id);
        if (profile == null)
        {
            return NotFound("Student profile not found.");
        }

        return View(profile);
    }
}

