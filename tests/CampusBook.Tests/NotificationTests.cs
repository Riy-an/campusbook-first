using CampusBook.DataAccess;
using CampusBook.DataAccess.Implementation;
using CampusBook.Entities;
using CampusBook.Facade.Implementation;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CampusBook.Tests;

public class NotificationTests
{
    private (NotificationFacade facade, NotificationRepository notifRepo, ApplicationDbContext context, User user1, User user2) SetupEnvironment(string dbName)
    {
        var context = TestDbContextFactory.Create(dbName);
        var notifRepo = new NotificationRepository(context);
        var logger = NullLogger<NotificationFacade>.Instance;

        var user1 = new User { UserId = 1, FullName = "Student One", StudentId = "STU-N1", Email = "n1@gmail.com", PasswordHash = "hash", RoleId = 1, IsActive = true };
        var user2 = new User { UserId = 2, FullName = "Student Two", StudentId = "STU-N2", Email = "n2@gmail.com", PasswordHash = "hash", RoleId = 1, IsActive = true };
        context.Users.AddRange(user1, user2);
        context.SaveChanges();

        var facade = new NotificationFacade(notifRepo, logger);
        return (facade, notifRepo, context, user1, user2);
    }

    [Fact]
    public async Task NotificationWorkflow_CreateAndCountUnread()
    {
        // Arrange
        var (facade, notifRepo, _, user1, _) = SetupEnvironment(Guid.NewGuid().ToString());

        // Act
        await notifRepo.AddAsync(new Notification { UserId = user1.UserId, Title = "New Request", Message = "Someone requested your book", Type = NotificationType.RequestReceived, NavigationUrl = "/requests/received" });
        await notifRepo.AddAsync(new Notification { UserId = user1.UserId, Title = "Book Accepted", Message = "Your request was accepted", Type = NotificationType.RequestAccepted, NavigationUrl = "/requests/sent" });

        // Assert
        var unreadCount = await facade.GetUnreadCountAsync(user1.UserId);
        Assert.Equal(2, unreadCount);

        var list = await facade.GetUserNotificationsAsync(user1.UserId);
        Assert.Equal(2, list.Notifications.Count());
        Assert.False(list.Notifications.First().IsRead);
    }

    [Fact]
    public async Task MarkAsReadAsync_OnlyOwnerCanMarkRead()
    {
        // Arrange
        var (facade, notifRepo, context, user1, user2) = SetupEnvironment(Guid.NewGuid().ToString());
        await notifRepo.AddAsync(new Notification { UserId = user1.UserId, Title = "Notice", Message = "Details", Type = NotificationType.General });
        var notif = context.Notifications.First(n => n.UserId == user1.UserId);

        // Act 1: User 2 tries to mark User 1's notification as read
        await facade.MarkAsReadAsync(notif.NotificationId, user2.UserId);
        var unrefreshed = await context.Notifications.FindAsync(notif.NotificationId);
        Assert.False(unrefreshed!.IsRead);

        // Act 2: User 1 marks own notification as read
        var successResult = await facade.MarkAsReadAsync(notif.NotificationId, user1.UserId);
        Assert.True(successResult.Success);

        var refreshed = await context.Notifications.FindAsync(notif.NotificationId);
        Assert.True(refreshed!.IsRead);
    }

    [Fact]
    public async Task MarkAllAsReadAsync_UpdatesAllUnreadForUser()
    {
        // Arrange
        var (facade, notifRepo, _, user1, user2) = SetupEnvironment(Guid.NewGuid().ToString());
        await notifRepo.AddAsync(new Notification { UserId = user1.UserId, Title = "N1", Message = "M1", Type = NotificationType.RequestReceived });
        await notifRepo.AddAsync(new Notification { UserId = user1.UserId, Title = "N2", Message = "M2", Type = NotificationType.RequestAccepted });
        await notifRepo.AddAsync(new Notification { UserId = user2.UserId, Title = "N3", Message = "M3", Type = NotificationType.RequestReceived });

        // Act
        var result = await facade.MarkAllAsReadAsync(user1.UserId);
        Assert.True(result.Success);

        // Assert
        var user1Unread = await facade.GetUnreadCountAsync(user1.UserId);
        var user2Unread = await facade.GetUnreadCountAsync(user2.UserId);

        Assert.Equal(0, user1Unread);
        Assert.Equal(1, user2Unread); // User 2's notification remains unread
    }
}
