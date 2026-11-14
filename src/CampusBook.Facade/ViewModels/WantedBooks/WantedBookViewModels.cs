using System.ComponentModel.DataAnnotations;
using CampusBook.Entities;
using CampusBook.Facade.ViewModels.Books;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CampusBook.Facade.ViewModels.WantedBooks;

public class WantedBookCreateViewModel
{
    [Required(ErrorMessage = "Book Title is required.")]
    [StringLength(250, ErrorMessage = "Title cannot exceed 250 characters.")]
    [Display(Name = "Book Title")]
    public string Title { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "Author cannot exceed 200 characters.")]
    [Display(Name = "Author (Optional)")]
    public string? Author { get; set; }

    [StringLength(50, ErrorMessage = "ISBN cannot exceed 50 characters.")]
    [Display(Name = "ISBN (Optional)")]
    public string? Isbn { get; set; }

    [Required(ErrorMessage = "Category is required.")]
    [Display(Name = "Category / Subject")]
    public int CategoryId { get; set; }

    [Display(Name = "Department (Optional)")]
    public int? DepartmentId { get; set; }

    [StringLength(50, ErrorMessage = "Course Code cannot exceed 50 characters.")]
    [Display(Name = "Course Code (Optional)")]
    public string? CourseCode { get; set; }

    [Display(Name = "Preferred Condition (Optional)")]
    public BookCondition? PreferredCondition { get; set; }

    [Range(0, 99999.99, ErrorMessage = "Budget must be positive.")]
    [Display(Name = "Maximum Budget (৳, Optional)")]
    public decimal? MaxBudget { get; set; }

    [Display(Name = "Preferred Listing Type")]
    public ExchangeType? PreferredExchangeType { get; set; } // Sale, Exchange, or null for Either

    [StringLength(1500, ErrorMessage = "Description cannot exceed 1500 characters.")]
    [Display(Name = "Additional Notes / Description (Optional)")]
    public string? Description { get; set; }

    public IEnumerable<SelectListItem> Categories { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> Departments { get; set; } = new List<SelectListItem>();
}

public class WantedBookEditViewModel : WantedBookCreateViewModel
{
    public int WantedBookId { get; set; }
    public WantedBookStatus Status { get; set; }
}

public class WantedBookItemViewModel
{
    public int WantedBookId { get; set; }
    public int UserId { get; set; }
    public string PosterName { get; set; } = string.Empty;
    public string PosterDepartment { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Author { get; set; }
    public string? Isbn { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? DepartmentName { get; set; }
    public string? CourseCode { get; set; }
    public BookCondition? PreferredCondition { get; set; }
    public decimal? MaxBudget { get; set; }
    public ExchangeType? PreferredExchangeType { get; set; }
    public string? Description { get; set; }
    public WantedBookStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsOwner { get; set; }
}

public class WantedBookDetailsViewModel : WantedBookItemViewModel
{
    public IEnumerable<BookItemViewModel> MatchingAvailableListings { get; set; } = new List<BookItemViewModel>();
}

public class WantedBookFilterViewModel
{
    public string? SearchTerm { get; set; }
    public int? CategoryId { get; set; }
    public int? DepartmentId { get; set; }

    public IEnumerable<WantedBookItemViewModel> Posts { get; set; } = new List<WantedBookItemViewModel>();
    public IEnumerable<SelectListItem> Categories { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> Departments { get; set; } = new List<SelectListItem>();
}
