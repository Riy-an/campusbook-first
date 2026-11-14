using CampusBook.DataAccess;
using CampusBook.DataAccess.Implementation;
using CampusBook.Entities;
using CampusBook.Facade.Implementation;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CampusBook.Tests;

public class MessagingTests
{
    private (ConversationFacade facade, ApplicationDbContext context, User owner, User requester, User outsider, Conversation conv, BookRequest req) SetupEnvironment(string dbName)
    {
        var context = TestDbContextFactory.Create(dbName);
        var convRepo = new ConversationRepository(context);
        var notifRepo = new NotificationRepository(context);
        var logger = NullLogger<ConversationFacade>.Instance;

        var owner = new User { UserId = 1, FullName = "Owner User", StudentId = "STU-M1", Email = "m1@gmail.com", PasswordHash = "hash", RoleId = 1, IsActive = true };
        var requester = new User { UserId = 2, FullName = "Requester User", StudentId = "STU-M2", Email = "m2@gmail.com", PasswordHash = "hash", RoleId = 1, IsActive = true };
        var outsider = new User { UserId = 3, FullName = "Outsider User", StudentId = "STU-M3", Email = "m3@gmail.com", PasswordHash = "hash", RoleId = 1, IsActive = true };
        context.Users.AddRange(owner, requester, outsider);

        var book = new Book { BookId = 1, Title = "Clean Architecture", Author = "Robert Martin", CategoryId = 1, Condition = BookCondition.Good, ExchangeType = ExchangeType.Sale, Price = 500, Description = "Desc", OwnerId = 1, Status = BookStatus.Reserved };
        context.Books.Add(book);

        var req = new BookRequest { RequestId = 1, BookId = 1, RequesterId = 2, Status = RequestStatus.Accepted, CreatedAt = DateTime.UtcNow };
        context.BookRequests.Add(req);

        var conv = new Conversation
        {
            ConversationId = 1,
            RequestId = 1,
            CreatedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow
        };
        context.Conversations.Add(conv);
        context.SaveChanges();

        var facade = new ConversationFacade(convRepo, notifRepo, logger);
        return (facade, context, owner, requester, outsider, conv, req);
    }

    [Fact]
    public async Task SendMessageAsync_ByAuthorizedRequester_Succeeds()
    {
        // Arrange
        var (facade, context, owner, requester, _, conv, _) = SetupEnvironment(Guid.NewGuid().ToString());

        // Act
        var result = await facade.SendMessageAsync(conv.ConversationId, requester.UserId, "Hi! Can we meet at the campus library at 3 PM?");

        // Assert
        Assert.True(result.Success);
        var messages = context.Messages.Where(m => m.ConversationId == conv.ConversationId).ToList();
        Assert.Single(messages);
        Assert.Equal("Hi! Can we meet at the campus library at 3 PM?", messages[0].Content);
        Assert.Equal(requester.UserId, messages[0].SenderId);

        // Owner should get a notification
        var ownerNotifs = context.Notifications.Where(n => n.UserId == owner.UserId).ToList();
        Assert.NotEmpty(ownerNotifs);
    }

    [Fact]
    public async Task SendMessageAsync_ByOutsider_IsRejected()
    {
        // Arrange
        var (facade, _, _, _, outsider, conv, _) = SetupEnvironment(Guid.NewGuid().ToString());

        // Act
        var result = await facade.SendMessageAsync(conv.ConversationId, outsider.UserId, "I want to intrude this chat.");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("not authorized", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SendMessageAsync_EmptyOrWhitespace_IsRejected()
    {
        // Arrange
        var (facade, _, _, requester, _, conv, _) = SetupEnvironment(Guid.NewGuid().ToString());

        // Act
        var result = await facade.SendMessageAsync(conv.ConversationId, requester.UserId, "     ");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("cannot be empty", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SendMessageAsync_SanitizesHtml()
    {
        // Arrange
        var (facade, context, _, requester, _, conv, _) = SetupEnvironment(Guid.NewGuid().ToString());

        // Act
        var result = await facade.SendMessageAsync(conv.ConversationId, requester.UserId, "Hello <script>alert('xss')</script> student!");

        // Assert
        Assert.True(result.Success);
        var msg = context.Messages.First(m => m.ConversationId == conv.ConversationId);
        Assert.DoesNotContain("<script>", msg.Content);
        Assert.Contains("&lt;script&gt;", msg.Content);
    }

    [Fact]
    public async Task SendMessageAsync_WhenRequestIsCompleted_IsReadOnly()
    {
        // Arrange
        var (facade, context, _, requester, _, conv, req) = SetupEnvironment(Guid.NewGuid().ToString());
        req.Status = RequestStatus.Completed;
        await context.SaveChangesAsync();

        // Act
        var result = await facade.SendMessageAsync(conv.ConversationId, requester.UserId, "Can I still reply?");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("read-only", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }
}
