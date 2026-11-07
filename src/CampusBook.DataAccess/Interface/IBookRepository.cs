using CampusBook.Entities;

namespace CampusBook.DataAccess.Interface;

public interface IBookRepository : IRepository<Book>
{
    Task<Book?> GetBookDetailsAsync(int bookId);
    Task<IEnumerable<Book>> GetUserBooksAsync(int userId);
    Task<IEnumerable<Book>> GetUserAvailableBooksAsync(int userId);
    Task<(IEnumerable<Book> Items, int TotalCount)> GetCatalogueBooksAsync(
        string? searchTerm,
        int? categoryId,
        int? departmentId,
        BookCondition? condition,
        ExchangeType? exchangeType,
        decimal? minPrice,
        decimal? maxPrice,
        string? sortBy,
        int pageIndex,
        int pageSize);
    Task<int> CountActiveListingsByUserAsync(int userId);
    Task<int> CountReservedListingsByUserAsync(int userId);
}

