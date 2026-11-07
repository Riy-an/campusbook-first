using System.ComponentModel.DataAnnotations;
using CampusBook.Facade.ViewModels.Books;
using Microsoft.AspNetCore.Http;

namespace CampusBook.Facade.ViewModels.Profile;

public class ProfileDetailsViewModel
{
    public int UserId { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Batch { get; set; } = string.Empty;
    public string? Semester { get; set; }
    public string? PhoneNumber { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public string? StudentIdCardUrl { get; set; }
    public string? Bio { get; set; }
    public bool IsEmailConfirmed { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int ActiveListingsCount { get; set; }
    public int CompletedExchangesCount { get; set; }
    public IEnumerable<BookItemViewModel> MyListings { get; set; } = new List<BookItemViewModel>();
}

public class ProfileEditViewModel
{
    // Read-only sensitive fields displayed on form
    public string StudentId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? CurrentProfilePictureUrl { get; set; }

    // Editable permitted fields
    [Required(ErrorMessage = "Full Name is required.")]
    [StringLength(100, ErrorMessage = "Full Name cannot exceed 100 characters.")]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Department is required.")]
    [StringLength(100, ErrorMessage = "Department cannot exceed 100 characters.")]
    [Display(Name = "Department")]
    public string Department { get; set; } = string.Empty;

    [Required(ErrorMessage = "Batch or Intake is required.")]
    [StringLength(50, ErrorMessage = "Batch cannot exceed 50 characters.")]
    [Display(Name = "Batch / Intake")]
    public string Batch { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "Semester cannot exceed 50 characters.")]
    [Display(Name = "Current Semester")]
    public string? Semester { get; set; }

    [Phone(ErrorMessage = "Please enter a valid phone number.")]
    [StringLength(25, ErrorMessage = "Phone number cannot exceed 25 characters.")]
    [Display(Name = "Contact Number (Optional)")]
    public string? PhoneNumber { get; set; }

    [StringLength(1000, ErrorMessage = "Bio cannot exceed 1000 characters.")]
    [Display(Name = "Short Biography")]
    public string? Bio { get; set; }

    [Display(Name = "Profile Photo (Optional)")]
    public IFormFile? ProfilePicture { get; set; }
}

public class PublicProfileViewModel
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Batch { get; set; } = string.Empty;
    public string? Semester { get; set; }
    public string? Bio { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public DateTime MemberSince { get; set; }
    public int CompletedExchangesCount { get; set; }
    public IEnumerable<BookItemViewModel> AvailableListings { get; set; } = new List<BookItemViewModel>();
}

