using CampusBook.Entities;

namespace CampusBook.DataAccess.Interface;

public interface IWishlistRepository : IRepository<WishlistItem>
{
    Task<bool> ExistsAsync(int userId, int bookId);
    Task<WishlistItem?> GetByUserAndBookAsync(int userId, int bookId);
    Task<IEnumerable<WishlistItem>> GetUserWishlistAsync(int userId);
    Task<int> CountUserWishlistAsync(int userId);
}
