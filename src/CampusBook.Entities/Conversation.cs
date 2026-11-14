namespace CampusBook.Entities;

public class Conversation
{
    public int ConversationId { get; set; }

    public int RequestId { get; set; }
    public virtual BookRequest Request { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
}
