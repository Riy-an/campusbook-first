namespace CampusBook.Entities;

public class Notification
{
    public int NotificationId { get; set; }

    public int UserId { get; set; }
    public virtual User User { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.General;

    public string? RelatedEntityType { get; set; }
    public int? RelatedEntityId { get; set; }
    public string? NavigationUrl { get; set; }

    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
