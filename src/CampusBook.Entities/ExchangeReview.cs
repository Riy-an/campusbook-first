namespace CampusBook.Entities;

public class ExchangeReview
{
    public int ReviewId { get; set; }

    public int RequestId { get; set; }
    public virtual BookRequest Request { get; set; } = null!;

    public int ReviewerId { get; set; }
    public virtual User Reviewer { get; set; } = null!;

    public int ReviewedUserId { get; set; }
    public virtual User ReviewedUser { get; set; } = null!;

    public int Rating { get; set; } // 1 - 5
    public string Comment { get; set; } = string.Empty;

    public bool IsHidden { get; set; } = false;
    public string? ModerationReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
