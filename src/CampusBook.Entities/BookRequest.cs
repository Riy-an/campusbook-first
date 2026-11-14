namespace CampusBook.Entities;

public class BookRequest
{
    public int RequestId { get; set; }

    public int BookId { get; set; }
    public virtual Book Book { get; set; } = null!;

    public int RequesterId { get; set; }
    public virtual User Requester { get; set; } = null!;

    public int? OfferedBookId { get; set; }
    public virtual Book? OfferedBook { get; set; }

    public string? RequestMessage { get; set; }
    public RequestStatus Status { get; set; } = RequestStatus.Pending;

    public bool OwnerConfirmed { get; set; }
    public DateTime? OwnerConfirmedAt { get; set; }

    public bool RequesterConfirmed { get; set; }
    public DateTime? RequesterConfirmedAt { get; set; }

    public virtual ExchangeOffer? ExchangeOffer { get; set; }
    public virtual Conversation? Conversation { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

