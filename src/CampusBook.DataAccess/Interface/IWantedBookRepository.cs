using CampusBook.Entities;

namespace CampusBook.DataAccess.Interface;

public interface IWantedBookRepository : IRepository<WantedBook>
{
    Task<WantedBook?> GetDetailsAsync(int wantedBookId);
    Task<IEnumerable<WantedBook>> GetByUserAsync(int userId);
    Task<IEnumerable<WantedBook>> GetActivePostsAsync(string? searchTerm, int? categoryId, int? departmentId);
    Task<IEnumerable<WantedBook>> FindMatchesForListingAsync(string title, string? author, string? isbn, int categoryId, decimal price, ExchangeType exchangeType);
    Task<IEnumerable<Book>> FindMatchingListingsForWantedBookAsync(WantedBook wantedBook);
    Task<int> CountActiveByUserAsync(int userId);
}
