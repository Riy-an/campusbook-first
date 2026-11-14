using System.Security.Claims;
using CampusBook.Facade.Interface;
using CampusBook.Facade.Services;
using CampusBook.Facade.ViewModels.Account;
using CampusBook.Framework;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusBook.Web.UI.Controllers;

public class AccountController : Controller
{
    private readonly IUserFacade _userFacade;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        IUserFacade userFacade,
        IEmailSender emailSender,
        ILogger<AccountController> logger)
    {
        _userFacade = userFacade;
        _emailSender = emailSender;
        _logger = logger;
    }

    [HttpGet]
    [Route(UrlRewrite.Account.Login)]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [Route(UrlRewrite.Account.Login)]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _userFacade.ValidateLoginAsync(model.Email, model.Password);
        if (!result.Success || result.Data == null)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Invalid login credentials.");
            return View(model);
        }

        var user = result.Data;

        // Establish claims identity
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role?.RoleName ?? "Student"),
            new("StudentId", user.StudentId),
            new("IsEmailConfirmed", user.IsEmailConfirmed.ToString())
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(7) : DateTimeOffset.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);
        _logger.LogInformation("Student {Email} logged in successfully.", user.Email);

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        TempData["SuccessMessage"] = $"Welcome back, {user.FullName}!";
        return RedirectToAction("Index", "Dashboard");
    }

    [HttpGet]
    [Route(UrlRewrite.Account.Register)]
    [AllowAnonymous]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        return View(new RegisterViewModel());
    }

    [HttpPost]
    [Route(UrlRewrite.Account.Register)]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var confirmationBaseUrl = $"{Request.Scheme}://{Request.Host}";
        var result = await _userFacade.RegisterAsync(model, confirmationBaseUrl);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Registration failed.");
            return View(model);
        }

        TempData["SuccessMessage"] = "Your student account has been registered successfully! You can now sign in with your credentials.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult RegistrationConfirmation()
    {
        ViewBag.RegisteredEmail = TempData["RegisteredEmail"];
        ViewBag.DevConfirmationLink = TempData["DevConfirmationLink"];
        return View();
    }

    [HttpGet]
    [Route(UrlRewrite.Account.ConfirmEmail)]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            ViewBag.Message = "Invalid confirmation token.";
            ViewBag.Success = false;
            return View();
        }

        var result = await _userFacade.ConfirmEmailAsync(token);
        ViewBag.Success = result.Success;
        ViewBag.Message = result.Success
            ? "Your university email has been verified successfully! You may now sign in."
            : result.ErrorMessage ?? "Email verification failed.";

        return View();
    }

    [HttpPost]
    [Route(UrlRewrite.Account.Logout)]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["SuccessMessage"] = "You have been logged out successfully.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [Route(UrlRewrite.Account.ForgotPassword)]
    [AllowAnonymous]
    public IActionResult ForgotPassword()
    {
        return View(new ForgotPasswordViewModel());
    }

    [HttpPost]
    [Route(UrlRewrite.Account.ForgotPassword)]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var resetBaseUrl = $"{Request.Scheme}://{Request.Host}";
        await _userFacade.GeneratePasswordResetTokenAsync(model.Email, resetBaseUrl);

        ViewBag.DevResetLink = _emailSender.LastGeneratedResetLink;
        ViewBag.Message = "If an active account exists for that email, a password reset link has been dispatched.";
        return View("ForgotPasswordConfirmation");
    }

    [HttpGet]
    [Route(UrlRewrite.Account.ResetPassword)]
    [AllowAnonymous]
    public IActionResult ResetPassword(string token, string email)
    {
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
        {
            return BadRequest("A valid reset token and email must be provided.");
        }

        return View(new ResetPasswordViewModel { Token = token, Email = email });
    }

    [HttpPost]
    [Route(UrlRewrite.Account.ResetPassword)]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _userFacade.ResetPasswordAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Password reset failed.");
            return View(model);
        }

        TempData["SuccessMessage"] = "Password has been successfully updated. You may now log in.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [Route(UrlRewrite.Account.AccessDenied)]
    public IActionResult AccessDenied()
    {
        return View();
    }
}

