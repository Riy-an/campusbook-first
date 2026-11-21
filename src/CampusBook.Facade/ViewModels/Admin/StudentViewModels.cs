using CampusBook.Entities;

namespace CampusBook.Facade.ViewModels.Admin;

public class StudentListViewModel
{
    public IEnumerable<User> Students { get; set; } = new List<User>();
    public int TotalCount { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 15;
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    public string? SearchTerm { get; set; }
    public bool? IsActive { get; set; }
    public StudentVerificationStatus? VerificationStatus { get; set; }
    public string? SortBy { get; set; }
}

public class StudentDetailsViewModel
{
    public User User { get; set; } = null!;
    public StudentProfile? Profile => User.StudentProfile;
    public int TotalListings { get; set; }
    public int TotalSentRequests { get; set; }
    public int CompletedExchanges { get; set; }
    public IEnumerable<string> EffectivePermissions { get; set; } = new List<string>();
}

public class StudentVerificationViewModel
{
    public int UserId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string UniversityEmail { get; set; } = string.Empty;
    public string? StudentIdCardUrl { get; set; }
    public StudentVerificationStatus Status { get; set; }
    public string? Notes { get; set; }
}

public class StudentSuspensionViewModel
{
    public int UserId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}
