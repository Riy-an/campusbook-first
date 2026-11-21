using CampusBook.Entities;

namespace CampusBook.Facade.ViewModels.Admin;

public class BookModerationListViewModel
{
    public IEnumerable<Book> Books { get; set; } = new List<Book>();
    public int TotalCount { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 15;
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    public string? SearchTerm { get; set; }
    public int? CategoryId { get; set; }
    public BookStatus? Status { get; set; }
    public IEnumerable<BookCategory> Categories { get; set; } = new List<BookCategory>();
}

public class BookModerationActionViewModel
{
    public int BookId { get; set; }
    public string Title { get; set; } = string.Empty;
    public BookStatus NewStatus { get; set; }
    public string? Reason { get; set; }
}
