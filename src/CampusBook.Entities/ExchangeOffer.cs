namespace CampusBook.Entities;

public class ExchangeOffer
{
    public int ExchangeOfferId { get; set; }

    public int RequestId { get; set; }
    public virtual BookRequest Request { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string? Isbn { get; set; }
    public string? Edition { get; set; }
    public string? PublicationYear { get; set; }

    public int CategoryId { get; set; }
    public virtual BookCategory Category { get; set; } = null!;

    public int? DepartmentId { get; set; }
    public virtual Department? Department { get; set; }

    public string? CourseCode { get; set; }
    public BookCondition Condition { get; set; } = BookCondition.Good;

    public string Description { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? ImageUrl { get; set; }

    public int? OfferedBookListingId { get; set; }
    public virtual Book? OfferedBookListing { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
