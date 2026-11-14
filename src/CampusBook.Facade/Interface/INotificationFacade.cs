using CampusBook.Facade.Common;
using CampusBook.Facade.ViewModels.Notifications;

namespace CampusBook.Facade.Interface;

public interface INotificationFacade
{
    Task<NotificationListViewModel> GetUserNotificationsAsync(int userId, int pageIndex = 1, int pageSize = 20);
    Task<int> GetUnreadCountAsync(int userId);
    Task<ServiceResult> MarkAsReadAsync(int notificationId, int userId);
    Task<ServiceResult> MarkAllAsReadAsync(int userId);
}
