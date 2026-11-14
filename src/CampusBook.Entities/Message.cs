namespace CampusBook.Entities;

public class Message
{
    public int MessageId { get; set; }

    public int ConversationId { get; set; }
    public virtual Conversation Conversation { get; set; } = null!;

    public int SenderId { get; set; }
    public virtual User Sender { get; set; } = null!;

    public string Content { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}
