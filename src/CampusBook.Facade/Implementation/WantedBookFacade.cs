using CampusBook.DataAccess.Interface;
using CampusBook.Entities;
using CampusBook.Facade.Common;
using CampusBook.Facade.Interface;
using CampusBook.Facade.ViewModels.Books;
using CampusBook.Facade.ViewModels.WantedBooks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;

namespace CampusBook.Facade.Implementation;

public class WantedBookFacade : IWantedBookFacade
{
    private readonly IWantedBookRepository _wantedBookRepository;
    private readonly ILookupRepository _lookupRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly ILogger<WantedBookFacade> _logger;

    public WantedBookFacade(
        IWantedBookRepository wantedBookRepository,
        ILookupRepository lookupRepository,
        INotificationRepository notificationRepository,
        ILogger<WantedBookFacade> logger)
    {
        _wantedBookRepository = wantedBookRepository;
        _lookupRepository = lookupRepository;
        _notificationRepository = notificationRepository;
        _logger = logger;
    }

    public async Task<WantedBookCreateViewModel> GetCreateViewModelAsync()
    {
        var categories = await _lookupRepository.GetAllCategoriesAsync();
        var departments = await _lookupRepository.GetAllDepartmentsAsync();

        return new WantedBookCreateViewModel
        {
            Categories = categories.Select(c => new SelectListItem { Value = c.CategoryId.ToString(), Text = c.Name }),
            Departments = departments.Select(d => new SelectListItem { Value = d.DepartmentId.ToString(), Text = $"{d.Code} - {d.Name}" })
        };
    }

    public async Task<ServiceResult<int>> CreateWantedBookAsync(int userId, WantedBookCreateViewModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (string.IsNullOrWhiteSpace(model.Title))
        {
            return ServiceResult<int>.Fail("Book title is required.");
        }

        if (model.CategoryId <= 0)
        {
            return ServiceResult<int>.Fail("Subject category is required.");
        }

        if (model.MaxBudget.HasValue && model.MaxBudget.Value < 0)
        {
            return ServiceResult<int>.Fail("Maximum budget cannot be negative.");
        }

        // Prevent Donation from being set
        if (model.PreferredExchangeType == ExchangeType.Donation)
        {
            model.PreferredExchangeType = null;
        }

        var post = new WantedBook
        {
            UserId = userId,
            Title = model.Title.Trim(),
            Author = string.IsNullOrWhiteSpace(model.Author) ? null : model.Author.Trim(),
            Isbn = string.IsNullOrWhiteSpace(model.Isbn) ? null : model.Isbn.Trim(),
            CategoryId = model.CategoryId,
            DepartmentId = model.DepartmentId > 0 ? model.DepartmentId : null,
            CourseCode = string.IsNullOrWhiteSpace(model.CourseCode) ? null : model.CourseCode.Trim(),
            PreferredCondition = model.PreferredCondition,
            MaxBudget = model.MaxBudget,
            PreferredExchangeType = model.PreferredExchangeType,
            Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
            Status = WantedBookStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        await _wantedBookRepository.AddAsync(post);
        _logger.LogInformation("User {UserId} created WantedBook post {WantedBookId}", userId, post.WantedBookId);

        return ServiceResult<int>.Ok(post.WantedBookId);
    }

    public async Task<WantedBookEditViewModel?> GetEditViewModelAsync(int wantedBookId, int userId)
    {
        var post = await _wantedBookRepository.GetDetailsAsync(wantedBookId);
        if (post == null || post.UserId != userId)
        {
            return null;
        }

        var categories = await _lookupRepository.GetAllCategoriesAsync();
        var departments = await _lookupRepository.GetAllDepartmentsAsync();

        return new WantedBookEditViewModel
        {
            WantedBookId = post.WantedBookId,
            Title = post.Title,
            Author = post.Author,
            Isbn = post.Isbn,
            CategoryId = post.CategoryId,
            DepartmentId = post.DepartmentId,
            CourseCode = post.CourseCode,
            PreferredCondition = post.PreferredCondition,
            MaxBudget = post.MaxBudget,
            PreferredExchangeType = post.PreferredExchangeType,
            Description = post.Description,
            Status = post.Status,
            Categories = categories.Select(c => new SelectListItem { Value = c.CategoryId.ToString(), Text = c.Name, Selected = c.CategoryId == post.CategoryId }),
            Departments = departments.Select(d => new SelectListItem { Value = d.DepartmentId.ToString(), Text = $"{d.Code} - {d.Name}", Selected = d.DepartmentId == post.DepartmentId })
        };
    }

    public async Task<ServiceResult> UpdateWantedBookAsync(int userId, WantedBookEditViewModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var post = await _wantedBookRepository.GetByIdAsync(model.WantedBookId);
        if (post == null)
        {
            return ServiceResult.Fail("Wanted book post not found.");
        }

        if (post.UserId != userId)
        {
            return ServiceResult.Fail("You are not authorized to edit this post.");
        }

        if (string.IsNullOrWhiteSpace(model.Title))
        {
            return ServiceResult.Fail("Book title is required.");
        }

        post.Title = model.Title.Trim();
        post.Author = string.IsNullOrWhiteSpace(model.Author) ? null : model.Author.Trim();
        post.Isbn = string.IsNullOrWhiteSpace(model.Isbn) ? null : model.Isbn.Trim();
        post.CategoryId = model.CategoryId;
        post.DepartmentId = model.DepartmentId > 0 ? model.DepartmentId : null;
        post.CourseCode = string.IsNullOrWhiteSpace(model.CourseCode) ? null : model.CourseCode.Trim();
        post.PreferredCondition = model.PreferredCondition;
        post.MaxBudget = model.MaxBudget;
        post.PreferredExchangeType = model.PreferredExchangeType == ExchangeType.Donation ? null : model.PreferredExchangeType;
        post.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
        post.Status = model.Status;
        post.UpdatedAt = DateTime.UtcNow;

        await _wantedBookRepository.UpdateAsync(post);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> CloseWantedBookAsync(int userId, int wantedBookId)
    {
        var post = await _wantedBookRepository.GetByIdAsync(wantedBookId);
        if (post == null)
        {
            return ServiceResult.Fail("Wanted book post not found.");
        }

        if (post.UserId != userId)
        {
            return ServiceResult.Fail("You are not authorized to close this post.");
        }

        post.Status = WantedBookStatus.Closed;
        post.UpdatedAt = DateTime.UtcNow;

        await _wantedBookRepository.UpdateAsync(post);
        return ServiceResult.Ok();
    }

    public async Task<IEnumerable<WantedBookItemViewModel>> GetMyWantedBooksAsync(int userId)
    {
        var posts = await _wantedBookRepository.GetByUserAsync(userId);
        return posts.Select(p => new WantedBookItemViewModel
        {
            WantedBookId = p.WantedBookId,
            UserId = p.UserId,
            PosterName = "You",
            PosterDepartment = p.Department?.Name ?? "General",
            Title = p.Title,
            Author = p.Author,
            Isbn = p.Isbn,
            CategoryName = p.Category?.Name ?? "General",
            DepartmentName = p.Department?.Name,
            CourseCode = p.CourseCode,
            PreferredCondition = p.PreferredCondition,
            MaxBudget = p.MaxBudget,
            PreferredExchangeType = p.PreferredExchangeType,
            Description = p.Description,
            Status = p.Status,
            CreatedAt = p.CreatedAt,
            IsOwner = true
        });
    }

    public async Task<WantedBookFilterViewModel> GetActiveWantedBooksAsync(string? searchTerm, int? categoryId, int? departmentId)
    {
        var posts = await _wantedBookRepository.GetActivePostsAsync(searchTerm, categoryId, departmentId);
        var categories = await _lookupRepository.GetAllCategoriesAsync();
        var departments = await _lookupRepository.GetAllDepartmentsAsync();

        return new WantedBookFilterViewModel
        {
            SearchTerm = searchTerm,
            CategoryId = categoryId,
            DepartmentId = departmentId,
            Posts = posts.Select(p => new WantedBookItemViewModel
            {
                WantedBookId = p.WantedBookId,
                UserId = p.UserId,
                PosterName = p.User?.FullName ?? "Student",
                PosterDepartment = p.User?.StudentProfile?.Department ?? "Student",
                Title = p.Title,
                Author = p.Author,
                Isbn = p.Isbn,
                CategoryName = p.Category?.Name ?? "General",
                DepartmentName = p.Department?.Name,
                CourseCode = p.CourseCode,
                PreferredCondition = p.PreferredCondition,
                MaxBudget = p.MaxBudget,
                PreferredExchangeType = p.PreferredExchangeType,
                Description = p.Description,
                Status = p.Status,
                CreatedAt = p.CreatedAt
            }),
            Categories = categories.Select(c => new SelectListItem { Value = c.CategoryId.ToString(), Text = c.Name, Selected = c.CategoryId == categoryId }),
            Departments = departments.Select(d => new SelectListItem { Value = d.DepartmentId.ToString(), Text = $"{d.Code} - {d.Name}", Selected = d.DepartmentId == departmentId })
        };
    }

    public async Task<WantedBookDetailsViewModel?> GetWantedBookDetailsAsync(int wantedBookId, int currentUserId)
    {
        var post = await _wantedBookRepository.GetDetailsAsync(wantedBookId);
        if (post == null) return null;

        var matchingListings = await _wantedBookRepository.FindMatchingListingsForWantedBookAsync(post);

        return new WantedBookDetailsViewModel
        {
            WantedBookId = post.WantedBookId,
            UserId = post.UserId,
            PosterName = post.User?.FullName ?? "Student",
            PosterDepartment = post.User?.StudentProfile?.Department ?? "Student",
            Title = post.Title,
            Author = post.Author,
            Isbn = post.Isbn,
            CategoryName = post.Category?.Name ?? "General",
            DepartmentName = post.Department?.Name,
            CourseCode = post.CourseCode,
            PreferredCondition = post.PreferredCondition,
            MaxBudget = post.MaxBudget,
            PreferredExchangeType = post.PreferredExchangeType,
            Description = post.Description,
            Status = post.Status,
            CreatedAt = post.CreatedAt,
            IsOwner = post.UserId == currentUserId,
            MatchingAvailableListings = matchingListings.Select(b => new BookItemViewModel
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
                Status = b.Status,
                OwnerName = b.Owner?.FullName ?? "Student",
                CreatedAt = b.CreatedAt
            })
        };
    }

    public async Task CheckAndNotifyMatchesForNewBookAsync(Book book)
    {
        try
        {
            var matches = await _wantedBookRepository.FindMatchesForListingAsync(
                book.Title,
                book.Author,
                book.Isbn,
                book.CategoryId,
                book.Price,
                book.ExchangeType);

            foreach (var match in matches)
            {
                // Don't notify the person who posted the book
                if (match.UserId == book.OwnerId) continue;

                await _notificationRepository.AddAsync(new Notification
                {
                    UserId = match.UserId,
                    Title = "Book Match Found! 📚",
                    Message = $"A student just listed \"{book.Title}\", which matches your active wanted book post!",
                    Type = NotificationType.WantedBookMatch,
                    RelatedEntityType = "Book",
                    RelatedEntityId = book.BookId,
                    NavigationUrl = $"/books/details/{book.BookId}",
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while matching wanted books for newly created book {BookId}", book.BookId);
        }
    }
}
