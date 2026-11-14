namespace CampusBook.Entities;

public class WantedBook
{
    public int WantedBookId { get; set; }

    public int UserId { get; set; }
    public virtual User User { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string? Author { get; set; }
    public string? Isbn { get; set; }

    public int CategoryId { get; set; }
    public virtual BookCategory Category { get; set; } = null!;

    public int? DepartmentId { get; set; }
    public virtual Department? Department { get; set; }

    public string? CourseCode { get; set; }
    public BookCondition? PreferredCondition { get; set; }

    public decimal? MaxBudget { get; set; }
    public ExchangeType? PreferredExchangeType { get; set; } // Sale, Exchange, or null for Either

    public string? Description { get; set; }
    public WantedBookStatus Status { get; set; } = WantedBookStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
