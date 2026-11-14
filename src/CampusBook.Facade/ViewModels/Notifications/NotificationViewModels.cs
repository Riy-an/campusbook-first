using CampusBook.Entities;

namespace CampusBook.Facade.ViewModels.Notifications;

public class NotificationItemViewModel
{
    public int NotificationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public string? RelatedEntityType { get; set; }
    public int? RelatedEntityId { get; set; }
    public string? NavigationUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class NotificationListViewModel
{
    public IEnumerable<NotificationItemViewModel> Notifications { get; set; } = new List<NotificationItemViewModel>();
    public int UnreadCount { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
