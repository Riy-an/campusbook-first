using CampusBook.DataAccess.Interface;
using CampusBook.Entities;
using CampusBook.Facade.Common;
using CampusBook.Facade.Interface;
using CampusBook.Facade.Services;
using CampusBook.Facade.ViewModels.Account;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CampusBook.Facade.Implementation;

public class UserFacade : IUserFacade
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<UserFacade> _logger;

    public UserFacade(
        IUserRepository userRepository,
        IPasswordHasher<User> passwordHasher,
        IEmailSender emailSender,
        IConfiguration configuration,
        IFileStorageService fileStorageService,
        ILogger<UserFacade> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _emailSender = emailSender;
        _configuration = configuration;
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    public async Task<ServiceResult<User>> RegisterAsync(RegisterViewModel model, string confirmationBaseUrl)
    {
        ArgumentNullException.ThrowIfNull(model);

        // 1. Check duplicate login email
        if (await _userRepository.EmailExistsAsync(model.Email))
        {
            return ServiceResult<User>.Fail("A student account with this login email address already exists.");
        }

        // 2. Check duplicate Student ID
        if (await _userRepository.StudentIdExistsAsync(model.StudentId))
        {
            return ServiceResult<User>.Fail("A student account with this Student ID already exists.");
        }

        // 3. Validate University Email is provided
        if (string.IsNullOrWhiteSpace(model.UniversityEmail))
        {
            return ServiceResult<User>.Fail("University email is required.");
        }

        // 4. Validate and handle required University ID Card upload
        if (model.StudentIdCard == null || model.StudentIdCard.Length == 0)
        {
            return ServiceResult<User>.Fail("University ID Card photo or scan is required for registration.");
        }

        if (!_fileStorageService.IsValidImage(model.StudentIdCard, out var imageError))
        {
            return ServiceResult<User>.Fail($"ID card image error: {imageError}");
        }

        var idCardUrl = await _fileStorageService.SaveFileAsync(model.StudentIdCard, "uploads/idcards");

        // 5. Retrieve or initialize Student role
        var studentRole = await _userRepository.GetRoleByNameAsync("Student");
        var roleId = studentRole?.RoleId ?? 1;

        // 6. Build user entity (immediately verified; no confirmation link required)
        var user = new User
        {
            FullName = model.FullName.Trim(),
            StudentId = model.StudentId.Trim(),
            Email = model.Email.Trim().ToLowerInvariant(),
            RoleId = roleId,
            IsEmailConfirmed = true,
            EmailConfirmationToken = null,
            EmailConfirmationTokenExpiry = null,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            StudentProfile = new StudentProfile
            {
                Department = model.Department.Trim(),
                Batch = model.Batch.Trim(),
                UniversityEmail = model.UniversityEmail.Trim().ToLowerInvariant(),
                PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim(),
                StudentIdCardUrl = idCardUrl,
                UpdatedAt = DateTime.UtcNow
            }
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);

        await _userRepository.AddAsync(user);
        _logger.LogInformation("Registered new student {Email} with ID {StudentId}", user.Email, user.StudentId);

        return ServiceResult<User>.Ok(user);
    }

    public async Task<ServiceResult<User>> ValidateLoginAsync(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return ServiceResult<User>.Fail("Email and password are required.");
        }

        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null || !user.IsActive)
        {
            return ServiceResult<User>.Fail("Invalid email or password.");
        }

        // Verify hashed password
        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            return ServiceResult<User>.Fail("Invalid email or password.");
        }

        return ServiceResult<User>.Ok(user);
    }

    public async Task<ServiceResult> ConfirmEmailAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return ServiceResult.Fail("Confirmation token is required.");
        }

        var user = await _userRepository.GetByConfirmationTokenAsync(token);
        if (user == null)
        {
            return ServiceResult.Fail("Invalid or expired confirmation link.");
        }

        if (user.EmailConfirmationTokenExpiry.HasValue && user.EmailConfirmationTokenExpiry.Value < DateTime.UtcNow)
        {
            return ServiceResult.Fail("Confirmation link has expired. Please request a new confirmation link.");
        }

        user.IsEmailConfirmed = true;
        user.EmailConfirmationToken = null;
        user.EmailConfirmationTokenExpiry = null;

        await _userRepository.UpdateAsync(user);
        _logger.LogInformation("Confirmed email for user {Email}", user.Email);

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> GeneratePasswordResetTokenAsync(string email, string resetBaseUrl)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return ServiceResult.Fail("Email is required.");
        }

        var user = await _userRepository.GetByEmailAsync(email);
        // Avoid leaking whether email exists
        if (user == null || !user.IsActive)
        {
            return ServiceResult.Ok();
        }

        user.PasswordResetToken = Guid.NewGuid().ToString("N");
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(2);
        await _userRepository.UpdateAsync(user);

        var resetLink = $"{resetBaseUrl.TrimEnd('/')}/reset-password?token={user.PasswordResetToken}&email={Uri.EscapeDataString(user.Email)}";
        var subject = "CampusBook Password Reset Request";
        var message = $"Hello {user.FullName},\n\nWe received a request to reset your password. " +
                      $"Click the link below to set a new password:\n\n{resetLink}\n\n" +
                      $"This link expires in 2 hours. If you did not request this, please ignore this email.\n\nCampusBook Team";

        await _emailSender.SendEmailAsync(user.Email, subject, message);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> ResetPasswordAsync(ResetPasswordViewModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var user = await _userRepository.GetByEmailAsync(model.Email);
        if (user == null || user.PasswordResetToken != model.Token)
        {
            return ServiceResult.Fail("Invalid or expired password reset token.");
        }

        if (user.PasswordResetTokenExpiry.HasValue && user.PasswordResetTokenExpiry.Value < DateTime.UtcNow)
        {
            return ServiceResult.Fail("Password reset token has expired.");
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, model.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;

        await _userRepository.UpdateAsync(user);
        _logger.LogInformation("Password reset successfully for user {Email}", user.Email);

        return ServiceResult.Ok();
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await _userRepository.GetUserWithProfileAsync(userId);
    }

    public async Task<bool> IsEmailConfirmedAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        return user != null;
    }
}

