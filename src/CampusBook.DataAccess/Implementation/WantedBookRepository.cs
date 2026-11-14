using CampusBook.DataAccess.Interface;
using CampusBook.Entities;
using Microsoft.EntityFrameworkCore;

namespace CampusBook.DataAccess.Implementation;

public class WantedBookRepository : Repository<WantedBook>, IWantedBookRepository
{
    public WantedBookRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<WantedBook?> GetDetailsAsync(int wantedBookId)
    {
        return await _dbContext.WantedBooks
            .Include(wb => wb.User)
                .ThenInclude(u => u.StudentProfile)
            .Include(wb => wb.Category)
            .Include(wb => wb.Department)
            .FirstOrDefaultAsync(wb => wb.WantedBookId == wantedBookId);
    }

    public async Task<IEnumerable<WantedBook>> GetByUserAsync(int userId)
    {
        return await _dbContext.WantedBooks
            .Include(wb => wb.Category)
            .Include(wb => wb.Department)
            .Where(wb => wb.UserId == userId)
            .OrderByDescending(wb => wb.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<WantedBook>> GetActivePostsAsync(string? searchTerm, int? categoryId, int? departmentId)
    {
        var query = _dbContext.WantedBooks
            .Include(wb => wb.User)
                .ThenInclude(u => u.StudentProfile)
            .Include(wb => wb.Category)
            .Include(wb => wb.Department)
            .Where(wb => wb.Status == WantedBookStatus.Active);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(wb =>
                wb.Title.ToLower().Contains(term) ||
                (wb.Author != null && wb.Author.ToLower().Contains(term)) ||
                (wb.Isbn != null && wb.Isbn.ToLower().Contains(term)) ||
                (wb.CourseCode != null && wb.CourseCode.ToLower().Contains(term)));
        }

        if (categoryId.HasValue && categoryId.Value > 0)
        {
            query = query.Where(wb => wb.CategoryId == categoryId.Value);
        }

        if (departmentId.HasValue && departmentId.Value > 0)
        {
            query = query.Where(wb => wb.DepartmentId == departmentId.Value);
        }

        return await query.OrderByDescending(wb => wb.CreatedAt).ToListAsync();
    }

    public async Task<IEnumerable<WantedBook>> FindMatchesForListingAsync(
        string title,
        string? author,
        string? isbn,
        int categoryId,
        decimal price,
        ExchangeType exchangeType)
    {
        var normalizedTitle = title.Trim().ToLower();
        var normalizedIsbn = string.IsNullOrWhiteSpace(isbn) ? null : isbn.Trim().Replace("-", "").ToLower();

        return await _dbContext.WantedBooks
            .Include(wb => wb.User)
            .Where(wb => wb.Status == WantedBookStatus.Active &&
                (
                    // Exact or normalized ISBN match (strongest)
                    (!string.IsNullOrEmpty(normalizedIsbn) && wb.Isbn != null && wb.Isbn.Replace("-", "").ToLower() == normalizedIsbn) ||
                    // Or category match + title match
                    (wb.CategoryId == categoryId &&
                        (wb.Title.ToLower().Contains(normalizedTitle) || normalizedTitle.Contains(wb.Title.ToLower())))
                ) &&
                // Preferred listing type match
                (wb.PreferredExchangeType == null || wb.PreferredExchangeType == exchangeType) &&
                // Budget match for Sale
                (exchangeType != ExchangeType.Sale || wb.MaxBudget == null || wb.MaxBudget.Value >= price)
            )
            .ToListAsync();
    }

    public async Task<IEnumerable<Book>> FindMatchingListingsForWantedBookAsync(WantedBook wantedBook)
    {
        var normalizedTitle = wantedBook.Title.Trim().ToLower();
        var normalizedIsbn = string.IsNullOrWhiteSpace(wantedBook.Isbn) ? null : wantedBook.Isbn.Trim().Replace("-", "").ToLower();

        return await _dbContext.Books
            .Include(b => b.Owner)
            .Include(b => b.Category)
            .Where(b => b.Status == BookStatus.Available &&
                        b.ExchangeType != ExchangeType.Donation &&
                        b.OwnerId != wantedBook.UserId &&
                (
                    (!string.IsNullOrEmpty(normalizedIsbn) && b.Isbn != null && b.Isbn.Replace("-", "").ToLower() == normalizedIsbn) ||
                    (b.CategoryId == wantedBook.CategoryId &&
                        (b.Title.ToLower().Contains(normalizedTitle) || normalizedTitle.Contains(b.Title.ToLower())))
                ) &&
                (wantedBook.PreferredExchangeType == null || b.ExchangeType == wantedBook.PreferredExchangeType) &&
                (b.ExchangeType != ExchangeType.Sale || wantedBook.MaxBudget == null || b.Price <= wantedBook.MaxBudget.Value)
            )
            .OrderByDescending(b => b.CreatedAt)
            .Take(10)
            .ToListAsync();
    }

    public async Task<int> CountActiveByUserAsync(int userId)
    {
        return await _dbContext.WantedBooks
            .CountAsync(wb => wb.UserId == userId && wb.Status == WantedBookStatus.Active);
    }
}
