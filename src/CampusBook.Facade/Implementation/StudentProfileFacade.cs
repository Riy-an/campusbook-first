using CampusBook.DataAccess.Interface;
using CampusBook.Entities;
using CampusBook.Facade.Common;
using CampusBook.Facade.Interface;
using CampusBook.Facade.Services;
using CampusBook.Facade.ViewModels.Books;
using CampusBook.Facade.ViewModels.Profile;
using Microsoft.Extensions.Logging;

namespace CampusBook.Facade.Implementation;

public class StudentProfileFacade : IStudentProfileFacade
{
    private readonly IUserRepository _userRepository;
    private readonly IStudentProfileRepository _profileRepository;
    private readonly IBookRepository _bookRepository;
    private readonly IBookRequestRepository _requestRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<StudentProfileFacade> _logger;

    public StudentProfileFacade(
        IUserRepository userRepository,
        IStudentProfileRepository profileRepository,
        IBookRepository bookRepository,
        IBookRequestRepository requestRepository,
        IFileStorageService fileStorageService,
        ILogger<StudentProfileFacade> logger)
    {
        _userRepository = userRepository;
        _profileRepository = profileRepository;
        _bookRepository = bookRepository;
        _requestRepository = requestRepository;
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    public async Task<ProfileDetailsViewModel?> GetMyProfileAsync(int userId)
    {
        var user = await _userRepository.GetUserWithProfileAsync(userId);
        if (user == null) return null;

        var myBooks = await _bookRepository.GetUserBooksAsync(userId);
        var activeCount = await _bookRepository.CountActiveListingsByUserAsync(userId);
        var completedCount = await _requestRepository.CountCompletedExchangesByUserAsync(userId);

        var profile = user.StudentProfile;

        return new ProfileDetailsViewModel
        {
            UserId = user.UserId,
            StudentId = user.StudentId,
            FullName = user.FullName,
            Email = user.Email,
            Department = profile?.Department ?? "Unassigned",
            Batch = profile?.Batch ?? "Unassigned",
            Semester = profile?.Semester,
            PhoneNumber = profile?.PhoneNumber,
            ProfilePictureUrl = profile?.ProfilePictureUrl,
            StudentIdCardUrl = profile?.StudentIdCardUrl,
            Bio = profile?.Bio,
            IsEmailConfirmed = user.IsEmailConfirmed,
            RoleName = user.Role?.RoleName ?? "Student",
            CreatedAt = user.CreatedAt,
            ActiveListingsCount = activeCount,
            CompletedExchangesCount = completedCount,
            MyListings = myBooks.Select(b => new BookItemViewModel
            {
                BookId = b.BookId,
                Title = b.Title,
                Author = b.Author,
                CategoryName = b.Category?.Name ?? "General",
                DepartmentName = b.Department?.Name,
                Condition = b.Condition,
                ExchangeType = b.ExchangeType,
                Price = b.Price,
                ImageUrl = b.ImageUrl,
                OwnerId = b.OwnerId,
                OwnerName = user.FullName,
                Status = b.Status,
                CreatedAt = b.CreatedAt
            })
        };
    }

    public async Task<ProfileEditViewModel?> GetProfileForEditAsync(int userId)
    {
        var user = await _userRepository.GetUserWithProfileAsync(userId);
        if (user == null) return null;

        var profile = user.StudentProfile;

        return new ProfileEditViewModel
        {
            StudentId = user.StudentId,
            Email = user.Email,
            FullName = user.FullName,
            Department = profile?.Department ?? string.Empty,
            Batch = profile?.Batch ?? string.Empty,
            Semester = profile?.Semester,
            PhoneNumber = profile?.PhoneNumber,
            Bio = profile?.Bio,
            CurrentProfilePictureUrl = profile?.ProfilePictureUrl
        };
    }

    public async Task<ServiceResult> UpdateProfileAsync(int userId, ProfileEditViewModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var user = await _userRepository.GetUserWithProfileAsync(userId);
        if (user == null)
        {
            return ServiceResult.Fail("User profile not found.");
        }

        // Only update permitted fields (anti-overposting)
        user.FullName = model.FullName.Trim();

        if (user.StudentProfile == null)
        {
            user.StudentProfile = new StudentProfile
            {
                UserId = user.UserId
            };
        }

        user.StudentProfile.Department = model.Department.Trim();
        user.StudentProfile.Batch = model.Batch.Trim();
        user.StudentProfile.Semester = string.IsNullOrWhiteSpace(model.Semester) ? null : model.Semester.Trim();
        user.StudentProfile.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim();
        user.StudentProfile.Bio = string.IsNullOrWhiteSpace(model.Bio) ? null : model.Bio.Trim();
        user.StudentProfile.UpdatedAt = DateTime.UtcNow;

        // Handle profile picture upload if provided
        if (model.ProfilePicture != null)
        {
            if (!_fileStorageService.IsValidImage(model.ProfilePicture, out var error))
            {
                return ServiceResult.Fail(error ?? "Invalid image file.");
            }

            // Remove old profile picture if exists
            _fileStorageService.DeleteFile(user.StudentProfile.ProfilePictureUrl);

            var savedPath = await _fileStorageService.SaveFileAsync(model.ProfilePicture, "profiles");
            user.StudentProfile.ProfilePictureUrl = savedPath;
        }

        await _userRepository.UpdateAsync(user);
        _logger.LogInformation("Updated profile for student {StudentId}", user.StudentId);

        return ServiceResult.Ok();
    }

    public async Task<PublicProfileViewModel?> GetPublicProfileAsync(int targetUserId)
    {
        var user = await _userRepository.GetUserWithProfileAsync(targetUserId);
        if (user == null || !user.IsActive) return null;

        var profile = user.StudentProfile;
        var availableBooks = await _bookRepository.GetUserAvailableBooksAsync(targetUserId);
        var completedCount = await _requestRepository.CountCompletedExchangesByUserAsync(targetUserId);

        return new PublicProfileViewModel
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Department = profile?.Department ?? "Unassigned",
            Batch = profile?.Batch ?? "Unassigned",
            Semester = profile?.Semester,
            Bio = profile?.Bio,
            ProfilePictureUrl = profile?.ProfilePictureUrl,
            MemberSince = user.CreatedAt,
            CompletedExchangesCount = completedCount,
            AvailableListings = availableBooks.Select(b => new BookItemViewModel
            {
                BookId = b.BookId,
                Title = b.Title,
                Author = b.Author,
                Condition = b.Condition,
                ExchangeType = b.ExchangeType,
                Price = b.Price,
                ImageUrl = b.ImageUrl,
                OwnerId = b.OwnerId,
                OwnerName = user.FullName,
                Status = b.Status,
                CreatedAt = b.CreatedAt
            })
        };
    }
}

