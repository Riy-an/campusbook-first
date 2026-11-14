using System.Net;
using CampusBook.DataAccess.Interface;
using CampusBook.Entities;
using CampusBook.Facade.Common;
using CampusBook.Facade.Interface;
using CampusBook.Facade.ViewModels.Messages;
using Microsoft.Extensions.Logging;

namespace CampusBook.Facade.Implementation;

public class ConversationFacade : IConversationFacade
{
    private readonly IConversationRepository _conversationRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly ILogger<ConversationFacade> _logger;

    public ConversationFacade(
        IConversationRepository conversationRepository,
        INotificationRepository notificationRepository,
        ILogger<ConversationFacade> logger)
    {
        _conversationRepository = conversationRepository;
        _notificationRepository = notificationRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<ConversationSummaryViewModel>> GetUserConversationsAsync(int userId)
    {
        var conversations = await _conversationRepository.GetUserConversationsAsync(userId);

        return conversations.Select(c =>
        {
            var isOwner = c.Request.Book.OwnerId == userId;
            var otherUser = isOwner ? c.Request.Requester : c.Request.Book.Owner;
            var lastMsg = c.Messages.OrderByDescending(m => m.SentAt).FirstOrDefault();
            var unreadCount = c.Messages.Count(m => m.SenderId != userId && !m.IsRead);

            return new ConversationSummaryViewModel
            {
                ConversationId = c.ConversationId,
                RequestId = c.RequestId,
                BookId = c.Request.BookId,
                BookTitle = c.Request.Book.Title,
                OtherPartyName = otherUser?.FullName ?? "Student",
                OtherPartyDepartment = otherUser?.StudentProfile?.Department ?? "Student",
                OtherPartyPhotoUrl = otherUser?.StudentProfile?.ProfilePictureUrl,
                LastMessageContent = lastMsg?.Content,
                LastActivityAt = c.LastActivityAt,
                UnreadCount = unreadCount,
                IsExchangeCompleted = c.Request.Status == RequestStatus.Completed
            };
        });
    }

    public async Task<ConversationDetailsViewModel?> GetConversationDetailsAsync(int conversationId, int userId)
    {
        var conversation = await _conversationRepository.GetByIdWithMessagesAsync(conversationId);
        if (conversation == null) return null;

        var isOwner = conversation.Request.Book.OwnerId == userId;
        var isRequester = conversation.Request.RequesterId == userId;

        // Strictly authorized participants only
        if (!isOwner && !isRequester)
        {
            return null;
        }

        // Mark messages from other user as read
        await _conversationRepository.MarkMessagesAsReadAsync(conversationId, userId);

        var otherUser = isOwner ? conversation.Request.Requester : conversation.Request.Book.Owner;

        return new ConversationDetailsViewModel
        {
            ConversationId = conversation.ConversationId,
            RequestId = conversation.RequestId,
            BookId = conversation.Request.BookId,
            BookTitle = conversation.Request.Book.Title,
            OtherPartyName = otherUser?.FullName ?? "Student",
            OtherPartyDepartment = otherUser?.StudentProfile?.Department ?? "Student",
            OtherPartyPhotoUrl = otherUser?.StudentProfile?.ProfilePictureUrl,
            IsExchangeCompleted = conversation.Request.Status == RequestStatus.Completed,
            Messages = conversation.Messages.Select(m => new MessageItemViewModel
            {
                MessageId = m.MessageId,
                SenderId = m.SenderId,
                SenderName = m.Sender?.FullName ?? "Student",
                Content = m.Content,
                SentAt = m.SentAt,
                IsRead = m.IsRead,
                IsFromCurrentUser = m.SenderId == userId
            })
        };
    }

    public async Task<ServiceResult> SendMessageAsync(int conversationId, int senderId, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return ServiceResult.Fail("Message content cannot be empty.");
        }

        var trimmed = content.Trim();
        if (trimmed.Length > 2000)
        {
            return ServiceResult.Fail("Message cannot exceed 2000 characters.");
        }

        var conversation = await _conversationRepository.GetByIdWithMessagesAsync(conversationId);
        if (conversation == null)
        {
            return ServiceResult.Fail("Conversation not found.");
        }

        var isOwner = conversation.Request.Book.OwnerId == senderId;
        var isRequester = conversation.Request.RequesterId == senderId;

        if (!isOwner && !isRequester)
        {
            return ServiceResult.Fail("You are not authorized to send messages in this conversation.");
        }

        // Messaging rules: active once request is accepted; closed/read-only once completed, rejected or cancelled
        if (conversation.Request.Status != RequestStatus.Accepted)
        {
            return ServiceResult.Fail(conversation.Request.Status == RequestStatus.Completed
                ? "This exchange is completed. Conversation is now in read-only mode."
                : "Messaging is only available for accepted exchange requests.");
        }

        // Sanitize HTML entities to prevent script injection
        var sanitized = WebUtility.HtmlEncode(trimmed);

        var message = new Message
        {
            ConversationId = conversationId,
            SenderId = senderId,
            Content = sanitized,
            IsRead = false,
            SentAt = DateTime.UtcNow
        };

        await _conversationRepository.AddMessageAsync(message);

        // Notify recipient of new message
        var recipientId = isOwner ? conversation.Request.RequesterId : conversation.Request.Book.OwnerId;
        var senderName = isOwner ? conversation.Request.Book.Owner.FullName : conversation.Request.Requester.FullName;

        await _notificationRepository.AddAsync(new Notification
        {
            UserId = recipientId,
            Title = "New Message Received 💬",
            Message = $"{senderName} sent you a message regarding \"{conversation.Request.Book.Title}\".",
            Type = NotificationType.General,
            RelatedEntityType = "Conversation",
            RelatedEntityId = conversationId,
            NavigationUrl = $"/messages/conversation/{conversationId}",
            CreatedAt = DateTime.UtcNow
        });

        _logger.LogInformation("Message sent in conversation {ConversationId} by user {SenderId}", conversationId, senderId);
        return ServiceResult.Ok();
    }

    public async Task<int> GetUnreadMessageCountAsync(int userId)
    {
        return await _conversationRepository.GetUnreadMessageCountAsync(userId);
    }
}
