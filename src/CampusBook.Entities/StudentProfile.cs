namespace CampusBook.Entities;

public class StudentProfile
{
    public int UserId { get; set; }
    public virtual User User { get; set; } = null!;

    public string Department { get; set; } = string.Empty;
    public string Batch { get; set; } = string.Empty;
    public string UniversityEmail { get; set; } = string.Empty;
    public string? Semester { get; set; }
    public string? PhoneNumber { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public string? StudentIdCardUrl { get; set; }
    public int IdCardChangeCount { get; set; } = 0;
    public string? Bio { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

