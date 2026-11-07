using CampusBook.DataAccess.Interface;
using CampusBook.Entities;
using CampusBook.Facade.Common;
using CampusBook.Facade.Interface;
using CampusBook.Facade.Services;
using CampusBook.Facade.ViewModels.Books;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;

namespace CampusBook.Facade.Implementation;

public class BookFacade : IBookFacade
{
    private readonly IBookRepository _bookRepository;
    private readonly IBookRequestRepository _requestRepository;
    private readonly ILookupRepository _lookupRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<BookFacade> _logger;

    public BookFacade(
        IBookRepository bookRepository,
        IBookRequestRepository requestRepository,
        ILookupRepository lookupRepository,
        IFileStorageService fileStorageService,
        ILogger<BookFacade> logger)
    {
        _bookRepository = bookRepository;
        _requestRepository = requestRepository;
        _lookupRepository = lookupRepository;
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    public async Task<BookFilterViewModel> GetCatalogueAsync(BookFilterViewModel filter)
    {
        filter.PageIndex = Math.Max(1, filter.PageIndex);
        filter.PageSize = filter.PageSize <= 0 ? 9 : filter.PageSize;

        var (books, totalCount) = await _bookRepository.GetCatalogueBooksAsync(
            filter.SearchTerm,
            filter.CategoryId,
            filter.DepartmentId,
            filter.Condition,
            filter.ExchangeType,
            filter.MinPrice,
            filter.MaxPrice,
            filter.SortBy,
            filter.PageIndex,
            filter.PageSize);

        var bookItems = books.Select(b => new BookItemViewModel
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
            OwnerName = b.Owner?.FullName ?? "Student",
            Status = b.Status,
            CreatedAt = b.CreatedAt
        }).ToList();

        filter.Books = new PaginatedList<BookItemViewModel>(bookItems, totalCount, filter.PageIndex, filter.PageSize);
        filter.Categories = await GetCategoriesDropdownAsync(filter.CategoryId);
        filter.Departments = await GetDepartmentsDropdownAsync(filter.DepartmentId);

        return filter;
    }

    public async Task<BookDetailsViewModel?> GetBookDetailsAsync(int bookId, int? currentUserId)
    {
        var book = await _bookRepository.GetBookDetailsAsync(bookId);
        if (book == null) return null;

        var hasActiveRequest = false;
        var isOwner = false;

        if (currentUserId.HasValue)
        {
            isOwner = book.OwnerId == currentUserId.Value;
            if (!isOwner)
            {
                hasActiveRequest = await _requestRepository.HasActiveRequestAsync(bookId, currentUserId.Value);
            }
        }

        return new BookDetailsViewModel
        {
            BookId = book.BookId,
            Title = book.Title,
            Author = book.Author,
            Isbn = book.Isbn,
            Edition = book.Edition,
            CourseCode = book.CourseCode,
            CategoryName = book.Category?.Name ?? "General",
            DepartmentName = book.Department?.Name,
            Condition = book.Condition,
            ExchangeType = book.ExchangeType,
            Price = book.Price,
            Description = book.Description,
            ImageUrl = book.ImageUrl,
            Status = book.Status,
            CreatedAt = book.CreatedAt,
            UpdatedAt = book.UpdatedAt,
            OwnerId = book.OwnerId,
            OwnerName = book.Owner?.FullName ?? "Unknown",
            OwnerDepartment = book.Owner?.StudentProfile?.Department ?? "Student",
            OwnerBatch = book.Owner?.StudentProfile?.Batch ?? "Intake",
            IsViewerAuthenticated = currentUserId.HasValue,
            IsOwner = isOwner,
            HasActiveRequest = hasActiveRequest
        };
    }

    public async Task<ServiceResult<int>> CreateBookAsync(int ownerId, BookCreateViewModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        // Pricing business rules
        if (model.ExchangeType == ExchangeType.Donation)
        {
            model.Price = 0;
        }
        else if (model.ExchangeType == ExchangeType.Exchange)
        {
            model.Price = 0;
        }
        else if (model.ExchangeType == ExchangeType.Sale && model.Price < 0)
        {
            return ServiceResult<int>.Fail("Sale price cannot be negative.");
        }

        // Validate Category exists
        var category = await _lookupRepository.GetCategoryByIdAsync(model.CategoryId);
        if (category == null)
        {
            return ServiceResult<int>.Fail("Selected category does not exist.");
        }

        // Image upload handling
        string? imageUrl = null;
        if (model.Image != null)
        {
            if (!_fileStorageService.IsValidImage(model.Image, out var imageError))
            {
                return ServiceResult<int>.Fail(imageError ?? "Invalid book image.");
            }
            imageUrl = await _fileStorageService.SaveFileAsync(model.Image, "books");
        }

        var book = new Book
        {
            Title = model.Title.Trim(),
            Author = model.Author.Trim(),
            Isbn = string.IsNullOrWhiteSpace(model.Isbn) ? null : model.Isbn.Trim(),
            Edition = string.IsNullOrWhiteSpace(model.Edition) ? null : model.Edition.Trim(),
            CourseCode = string.IsNullOrWhiteSpace(model.CourseCode) ? null : model.CourseCode.Trim(),
            CategoryId = model.CategoryId,
            DepartmentId = model.DepartmentId > 0 ? model.DepartmentId : null,
            Condition = model.Condition,
            ExchangeType = model.ExchangeType,
            Price = model.Price,
            Description = model.Description.Trim(),
            ImageUrl = imageUrl,
            OwnerId = ownerId,
            Status = BookStatus.Available,
            CreatedAt = DateTime.UtcNow
        };

        await _bookRepository.AddAsync(book);
        _logger.LogInformation("Student {OwnerId} published book {BookId} ({Title})", ownerId, book.BookId, book.Title);

        return ServiceResult<int>.Ok(book.BookId);
    }

    public async Task<BookEditViewModel?> GetBookForEditAsync(int bookId, int ownerId)
    {
        var book = await _bookRepository.GetBookDetailsAsync(bookId);
        if (book == null || book.OwnerId != ownerId)
        {
            return null;
        }

        return new BookEditViewModel
        {
            BookId = book.BookId,
            Title = book.Title,
            Author = book.Author,
            Isbn = book.Isbn,
            Edition = book.Edition,
            CourseCode = book.CourseCode,
            CategoryId = book.CategoryId,
            DepartmentId = book.DepartmentId,
            Condition = book.Condition,
            ExchangeType = book.ExchangeType,
            Price = book.Price,
            Description = book.Description,
            ExistingImageUrl = book.ImageUrl,
            Status = book.Status,
            Categories = await GetCategoriesDropdownAsync(book.CategoryId),
            Departments = await GetDepartmentsDropdownAsync(book.DepartmentId)
        };
    }

    public async Task<ServiceResult> UpdateBookAsync(int ownerId, BookEditViewModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var book = await _bookRepository.GetByIdAsync(model.BookId);
        if (book == null)
        {
            return ServiceResult.Fail("Book listing not found.");
        }

        // Ownership rule
        if (book.OwnerId != ownerId)
        {
            return ServiceResult.Fail("You are not authorized to edit this book listing.");
        }

        // Cannot edit if currently reserved
        if (book.Status == BookStatus.Reserved)
        {
            return ServiceResult.Fail("This book cannot be modified while it is reserved for an active exchange.");
        }

        // Pricing rules
        if (model.ExchangeType == ExchangeType.Donation)
        {
            model.Price = 0;
        }
        else if (model.ExchangeType == ExchangeType.Exchange)
        {
            model.Price = 0;
        }
        else if (model.ExchangeType == ExchangeType.Sale && model.Price < 0)
        {
            return ServiceResult.Fail("Sale price cannot be negative.");
        }

        // Handle image replacement if provided
        if (model.Image != null)
        {
            if (!_fileStorageService.IsValidImage(model.Image, out var imageError))
            {
                return ServiceResult.Fail(imageError ?? "Invalid book image.");
            }

            _fileStorageService.DeleteFile(book.ImageUrl);
            book.ImageUrl = await _fileStorageService.SaveFileAsync(model.Image, "books");
        }

        book.Title = model.Title.Trim();
        book.Author = model.Author.Trim();
        book.Isbn = string.IsNullOrWhiteSpace(model.Isbn) ? null : model.Isbn.Trim();
        book.Edition = string.IsNullOrWhiteSpace(model.Edition) ? null : model.Edition.Trim();
        book.CourseCode = string.IsNullOrWhiteSpace(model.CourseCode) ? null : model.CourseCode.Trim();
        book.CategoryId = model.CategoryId;
        book.DepartmentId = model.DepartmentId > 0 ? model.DepartmentId : null;
        book.Condition = model.Condition;
        book.ExchangeType = model.ExchangeType;
        book.Price = model.Price;
        book.Description = model.Description.Trim();
        book.UpdatedAt = DateTime.UtcNow;

        await _bookRepository.UpdateAsync(book);
        _logger.LogInformation("Updated book listing {BookId} by owner {OwnerId}", book.BookId, ownerId);

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> DeactivateBookAsync(int bookId, int ownerId)
    {
        var book = await _bookRepository.GetByIdAsync(bookId);
        if (book == null)
        {
            return ServiceResult.Fail("Book listing not found.");
        }

        if (book.OwnerId != ownerId)
        {
            return ServiceResult.Fail("You are not authorized to deactivate this listing.");
        }

        if (book.Status == BookStatus.Reserved)
        {
            return ServiceResult.Fail("This listing cannot be deactivated while it is reserved for an active exchange.");
        }

        book.Status = BookStatus.Inactive;
        book.UpdatedAt = DateTime.UtcNow;
        await _bookRepository.UpdateAsync(book);

        _logger.LogInformation("Deactivated book listing {BookId} by owner {OwnerId}", bookId, ownerId);
        return ServiceResult.Ok();
    }

    public async Task<IEnumerable<BookItemViewModel>> GetUserBooksAsync(int userId)
    {
        var books = await _bookRepository.GetUserBooksAsync(userId);
        return books.Select(b => new BookItemViewModel
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
            Status = b.Status,
            CreatedAt = b.CreatedAt
        });
    }

    public async Task<IEnumerable<SelectListItem>> GetUserAvailableBooksDropdownAsync(int userId, int? selectedBookId = null)
    {
        var books = await _bookRepository.GetUserAvailableBooksAsync(userId);
        return books.Select(b => new SelectListItem
        {
            Value = b.BookId.ToString(),
            Text = $"{b.Title} ({b.Condition})",
            Selected = selectedBookId.HasValue && selectedBookId.Value == b.BookId
        });
    }

    public async Task<IEnumerable<SelectListItem>> GetCategoriesDropdownAsync(int? selectedCategoryId = null)
    {
        var categories = await _lookupRepository.GetAllCategoriesAsync();
        return categories.Select(c => new SelectListItem
        {
            Value = c.CategoryId.ToString(),
            Text = c.Name,
            Selected = selectedCategoryId.HasValue && selectedCategoryId.Value == c.CategoryId
        });
    }

    public async Task<IEnumerable<SelectListItem>> GetDepartmentsDropdownAsync(int? selectedDepartmentId = null)
    {
        var departments = await _lookupRepository.GetAllDepartmentsAsync();
        return departments.Select(d => new SelectListItem
        {
            Value = d.DepartmentId.ToString(),
            Text = $"{d.Name} ({d.Code})",
            Selected = selectedDepartmentId.HasValue && selectedDepartmentId.Value == d.DepartmentId
        });
    }
}

