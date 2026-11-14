using System.ComponentModel.DataAnnotations;
using CampusBook.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CampusBook.Facade.ViewModels.Books;

public class BookCreateViewModel
{
    [Required(ErrorMessage = "Book Title is required.")]
    [StringLength(250, ErrorMessage = "Title cannot exceed 250 characters.")]
    [Display(Name = "Book Title")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Author is required.")]
    [StringLength(200, ErrorMessage = "Author cannot exceed 200 characters.")]
    [Display(Name = "Author")]
    public string Author { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "ISBN cannot exceed 50 characters.")]
    [Display(Name = "ISBN (Optional)")]
    public string? Isbn { get; set; }

    [StringLength(50, ErrorMessage = "Edition cannot exceed 50 characters.")]
    [Display(Name = "Edition / Year (Optional)")]
    public string? Edition { get; set; }

    [Required(ErrorMessage = "Category is required.")]
    [Display(Name = "Category")]
    public int CategoryId { get; set; }

    [Display(Name = "Department (Optional)")]
    public int? DepartmentId { get; set; }

    [StringLength(50, ErrorMessage = "Course Code cannot exceed 50 characters.")]
    [Display(Name = "Course Code (Optional)")]
    public string? CourseCode { get; set; }

    [Required(ErrorMessage = "Book Condition is required.")]
    [Display(Name = "Book Condition")]
    public BookCondition Condition { get; set; } = BookCondition.Good;

    [Required(ErrorMessage = "Listing Type is required.")]
    [Display(Name = "Listing Type")]
    public ExchangeType ExchangeType { get; set; } = ExchangeType.Sale;

    [Range(0, 99999.99, ErrorMessage = "Price must be a positive number.")]
    [Display(Name = "Price (৳)")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "Description is required.")]
    [StringLength(2500, ErrorMessage = "Description cannot exceed 2500 characters.")]
    [Display(Name = "Description")]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Book Cover Image (Optional)")]
    public IFormFile? Image { get; set; }

    // Dropdown helpers
    public IEnumerable<SelectListItem> Categories { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> Departments { get; set; } = new List<SelectListItem>();
}

public class BookEditViewModel : BookCreateViewModel
{
    public int BookId { get; set; }
    public string? ExistingImageUrl { get; set; }
    public BookStatus Status { get; set; }
}

public class BookDetailsViewModel
{
    public int BookId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string? Isbn { get; set; }
    public string? Edition { get; set; }
    public string? CourseCode { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? DepartmentName { get; set; }
    public BookCondition Condition { get; set; }
    public ExchangeType ExchangeType { get; set; }
    public decimal Price { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public BookStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Owner details (permitted for public viewing)
    public int OwnerId { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public string OwnerDepartment { get; set; } = string.Empty;
    public string OwnerBatch { get; set; } = string.Empty;

    // Viewing student context & permissions
    public bool IsViewerAuthenticated { get; set; }
    public bool IsOwner { get; set; }
    public bool HasActiveRequest { get; set; }
    public bool CanRequest => IsViewerAuthenticated && !IsOwner && Status == BookStatus.Available && !HasActiveRequest;
}

public class BookFilterViewModel
{
    public string? SearchTerm { get; set; }
    public int? CategoryId { get; set; }
    public int? DepartmentId { get; set; }
    public BookCondition? Condition { get; set; }
    public ExchangeType? ExchangeType { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? SortBy { get; set; } = "newest";

    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 9;

    public PaginatedList<BookItemViewModel>? Books { get; set; }
    public IEnumerable<SelectListItem> Categories { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> Departments { get; set; } = new List<SelectListItem>();
}

