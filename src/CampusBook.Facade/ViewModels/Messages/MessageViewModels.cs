using System.ComponentModel.DataAnnotations;

namespace CampusBook.Facade.ViewModels.Messages;

public class MessageItemViewModel
{
    public int MessageId { get; set; }
    public int SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public bool IsRead { get; set; }
    public bool IsFromCurrentUser { get; set; }
}

public class ConversationSummaryViewModel
{
    public int ConversationId { get; set; }
    public int RequestId { get; set; }
    public int BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public string OtherPartyName { get; set; } = string.Empty;
    public string OtherPartyDepartment { get; set; } = string.Empty;
    public string? OtherPartyPhotoUrl { get; set; }
    public string? LastMessageContent { get; set; }
    public DateTime LastActivityAt { get; set; }
    public int UnreadCount { get; set; }
    public bool IsExchangeCompleted { get; set; }
}

public class ConversationDetailsViewModel
{
    public int ConversationId { get; set; }
    public int RequestId { get; set; }
    public int BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public string OtherPartyName { get; set; } = string.Empty;
    public string OtherPartyDepartment { get; set; } = string.Empty;
    public string? OtherPartyPhotoUrl { get; set; }
    public bool IsExchangeCompleted { get; set; }
    public IEnumerable<MessageItemViewModel> Messages { get; set; } = new List<MessageItemViewModel>();

    [Required(ErrorMessage = "Message content cannot be empty.")]
    [StringLength(2000, MinimumLength = 1, ErrorMessage = "Message must be between 1 and 2000 characters.")]
    public string NewMessageContent { get; set; } = string.Empty;
}
