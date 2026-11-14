using CampusBook.DataAccess.Interface;
using CampusBook.Entities;
using Microsoft.EntityFrameworkCore;

namespace CampusBook.DataAccess.Implementation;

public class WishlistRepository : Repository<WishlistItem>, IWishlistRepository
{
    public WishlistRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<bool> ExistsAsync(int userId, int bookId)
    {
        return await _dbContext.WishlistItems
            .AnyAsync(w => w.UserId == userId && w.BookId == bookId);
    }

    public async Task<WishlistItem?> GetByUserAndBookAsync(int userId, int bookId)
    {
        return await _dbContext.WishlistItems
            .FirstOrDefaultAsync(w => w.UserId == userId && w.BookId == bookId);
    }

    public async Task<IEnumerable<WishlistItem>> GetUserWishlistAsync(int userId)
    {
        return await _dbContext.WishlistItems
            .Include(w => w.Book)
                .ThenInclude(b => b.Owner)
            .Include(w => w.Book)
                .ThenInclude(b => b.Category)
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> CountUserWishlistAsync(int userId)
    {
        return await _dbContext.WishlistItems
            .CountAsync(w => w.UserId == userId);
    }
}
