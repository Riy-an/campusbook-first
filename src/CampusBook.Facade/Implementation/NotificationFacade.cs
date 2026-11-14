using CampusBook.DataAccess.Interface;
using CampusBook.Facade.Common;
using CampusBook.Facade.Interface;
using CampusBook.Facade.ViewModels.Notifications;
using Microsoft.Extensions.Logging;

namespace CampusBook.Facade.Implementation;

public class NotificationFacade : INotificationFacade
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ILogger<NotificationFacade> _logger;

    public NotificationFacade(
        INotificationRepository notificationRepository,
        ILogger<NotificationFacade> logger)
    {
        _notificationRepository = notificationRepository;
        _logger = logger;
    }

    public async Task<NotificationListViewModel> GetUserNotificationsAsync(int userId, int pageIndex = 1, int pageSize = 20)
    {
        pageIndex = Math.Max(1, pageIndex);
        pageSize = Math.Max(1, Math.Min(50, pageSize));

        var notifications = await _notificationRepository.GetUserNotificationsAsync(userId, pageIndex, pageSize);
        var unreadCount = await _notificationRepository.GetUnreadCountAsync(userId);

        return new NotificationListViewModel
        {
            Notifications = notifications.Select(n => new NotificationItemViewModel
            {
                NotificationId = n.NotificationId,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                RelatedEntityType = n.RelatedEntityType,
                RelatedEntityId = n.RelatedEntityId,
                NavigationUrl = n.NavigationUrl,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            }),
            UnreadCount = unreadCount,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _notificationRepository.GetUnreadCountAsync(userId);
    }

    public async Task<ServiceResult> MarkAsReadAsync(int notificationId, int userId)
    {
        await _notificationRepository.MarkAsReadAsync(notificationId, userId);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> MarkAllAsReadAsync(int userId)
    {
        await _notificationRepository.MarkAllAsReadAsync(userId);
        return ServiceResult.Ok();
    }
}
