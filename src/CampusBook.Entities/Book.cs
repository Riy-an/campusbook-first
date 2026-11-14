using System.ComponentModel.DataAnnotations;

namespace CampusBook.Entities;

public class Book
{
    public int BookId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string? Isbn { get; set; }
    public string? Edition { get; set; }
    public string? CourseCode { get; set; }

    public int CategoryId { get; set; }
    public virtual BookCategory Category { get; set; } = null!;

    public int? DepartmentId { get; set; }
    public virtual Department? Department { get; set; }

    public BookCondition Condition { get; set; } = BookCondition.Good;
    public ExchangeType ExchangeType { get; set; } = ExchangeType.Sale;
    public decimal Price { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }

    public int OwnerId { get; set; }
    public virtual User Owner { get; set; } = null!;

    public BookStatus Status { get; set; } = BookStatus.Available;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public virtual ICollection<BookRequest> Requests { get; set; } = new List<BookRequest>();
    public virtual ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();
    public virtual ICollection<ExchangeOffer> ExchangeOffersAsOfferedListing { get; set; } = new List<ExchangeOffer>();
}

