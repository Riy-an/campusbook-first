using CampusBook.Entities;

namespace CampusBook.Facade.ViewModels.Books;

public class PaginatedList<T>
{
    public List<T> Items { get; }
    public int PageIndex { get; }
    public int TotalPages { get; }
    public int TotalCount { get; }

    public PaginatedList(List<T> items, int count, int pageIndex, int pageSize)
    {
        PageIndex = pageIndex;
        TotalPages = (int)Math.Ceiling(count / (double)pageSize);
        TotalCount = count;
        Items = items;
    }

    public bool HasPreviousPage => PageIndex > 1;
    public bool HasNextPage => PageIndex < TotalPages;
}

public class BookItemViewModel
{
    public int BookId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? DepartmentName { get; set; }
    public BookCondition Condition { get; set; }
    public ExchangeType ExchangeType { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public int OwnerId { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public BookStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

