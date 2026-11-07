using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace CampusBook.Facade.ViewModels.Account;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Full Name is required.")]
    [StringLength(100, ErrorMessage = "Full Name cannot exceed 100 characters.")]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Student ID is required.")]
    [StringLength(50, ErrorMessage = "Student ID cannot exceed 50 characters.")]
    [Display(Name = "Student ID")]
    public string StudentId { get; set; } = string.Empty;

    [Required(ErrorMessage = "University Email is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
    [Display(Name = "University Email")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Please enter a valid contact number.")]
    [StringLength(25, ErrorMessage = "Contact number cannot exceed 25 characters.")]
    [Display(Name = "Contact No.")]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long.")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your password.")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Department is required.")]
    [StringLength(100, ErrorMessage = "Department cannot exceed 100 characters.")]
    [Display(Name = "Department")]
    public string Department { get; set; } = string.Empty;

    [Required(ErrorMessage = "Batch or Intake is required.")]
    [StringLength(50, ErrorMessage = "Batch/Intake cannot exceed 50 characters.")]
    [Display(Name = "Batch / Intake")]
    public string Batch { get; set; } = string.Empty;

    [Display(Name = "University ID Card Photo (Optional)")]
    public IFormFile? StudentIdCard { get; set; }
}
