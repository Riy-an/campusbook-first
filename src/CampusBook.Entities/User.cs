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
    public string? SuspensionReason { get; set; }
    public DateTime? SuspendedAt { get; set; }
    public int? SuspendedByAdminId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual StudentProfile? StudentProfile { get; set; }
    public virtual ICollection<Book> Books { get; set; } = new List<Book>();
    public virtual ICollection<BookRequest> SentRequests { get; set; } = new List<BookRequest>();
    public virtual ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();
    public virtual ICollection<WantedBook> WantedBooks { get; set; } = new List<WantedBook>();
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public virtual ICollection<Message> SentMessages { get; set; } = new List<Message>();
    public virtual ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public virtual ICollection<Report> ReportsSubmitted { get; set; } = new List<Report>();
    public virtual ICollection<Report> ReportsAssigned { get; set; } = new List<Report>();
    public virtual ICollection<ExchangeReview> ReviewsGiven { get; set; } = new List<ExchangeReview>();
    public virtual ICollection<ExchangeReview> ReviewsReceived { get; set; } = new List<ExchangeReview>();
}

