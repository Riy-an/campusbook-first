namespace CampusBook.Entities;

public class User
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public virtual Role Role { get; set; } = null!;

    public bool IsEmailConfirmed { get; set; }
    public string? EmailConfirmationToken { get; set; }
    public DateTime? EmailConfirmationTokenExpiry { get; set; }

    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiry { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual StudentProfile? StudentProfile { get; set; }
    public virtual ICollection<Book> Books { get; set; } = new List<Book>();
    public virtual ICollection<BookRequest> SentRequests { get; set; } = new List<BookRequest>();
}

