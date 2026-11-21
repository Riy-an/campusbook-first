using System.ComponentModel.DataAnnotations;
using CampusBook.Entities;

namespace CampusBook.Facade.ViewModels.Admin;

public class CategoryManagementViewModel
{
    public IEnumerable<(BookCategory Category, int BookCount)> Categories { get; set; } = new List<(BookCategory, int)>();
}

public class CategoryEditViewModel
{
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Category name is required")]
    [StringLength(100, ErrorMessage = "Category name cannot exceed 100 characters")]
    public string Name { get; set; } = string.Empty;

    [StringLength(250, ErrorMessage = "Description cannot exceed 250 characters")]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

public class DepartmentManagementViewModel
{
    public IEnumerable<(Department Department, int CourseCount, int BookCount)> Departments { get; set; } = new List<(Department, int, int)>();
}

public class DepartmentEditViewModel
{
    public int DepartmentId { get; set; }

    [Required(ErrorMessage = "Department name is required")]
    [StringLength(100, ErrorMessage = "Department name cannot exceed 100 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Department code is required")]
    [StringLength(20, ErrorMessage = "Department code cannot exceed 20 characters")]
    public string Code { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

public class CourseManagementViewModel
{
    public IEnumerable<Course> Courses { get; set; } = new List<Course>();
    public IEnumerable<Department> Departments { get; set; } = new List<Department>();
    public int? SelectedDepartmentId { get; set; }
}

public class CourseEditViewModel
{
    public int CourseId { get; set; }

    [Required(ErrorMessage = "Department is required")]
    public int DepartmentId { get; set; }

    [Required(ErrorMessage = "Course code is required")]
    [StringLength(20, ErrorMessage = "Course code cannot exceed 20 characters")]
    public string CourseCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Course name is required")]
    [StringLength(150, ErrorMessage = "Course name cannot exceed 150 characters")]
    public string CourseName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
