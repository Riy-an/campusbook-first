using CampusBook.DataAccess.Interface;
using CampusBook.Entities;
using Microsoft.EntityFrameworkCore;

namespace CampusBook.DataAccess.Implementation;

public class BookRepository : Repository<Book>, IBookRepository
{
    public BookRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<Book?> GetBookDetailsAsync(int bookId)
    {
        return await _dbContext.Books
            .Include(b => b.Category)
            .Include(b => b.Department)
            .Include(b => b.Owner)
                .ThenInclude(o => o.StudentProfile)
            .FirstOrDefaultAsync(b => b.BookId == bookId);
    }

    public async Task<IEnumerable<Book>> GetUserBooksAsync(int userId)
    {
        return await _dbContext.Books
            .Include(b => b.Category)
            .Include(b => b.Department)
            .Where(b => b.OwnerId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Book>> GetUserAvailableBooksAsync(int userId)
    {
        return await _dbContext.Books
            .Where(b => b.OwnerId == userId && b.Status == BookStatus.Available)
            .OrderBy(b => b.Title)
            .ToListAsync();
    }

    public async Task<(IEnumerable<Book> Items, int TotalCount)> GetCatalogueBooksAsync(
        string? searchTerm,
        int? categoryId,
        int? departmentId,
        BookCondition? condition,
        ExchangeType? exchangeType,
        decimal? minPrice,
        decimal? maxPrice,
        string? sortBy,
        int pageIndex,
        int pageSize)
    {
        IQueryable<Book> query = _dbContext.Books
            .Include(b => b.Category)
            .Include(b => b.Department)
            .Include(b => b.Owner)
            .Where(b => b.Status == BookStatus.Available);

        // Search term
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(b =>
                b.Title.ToLower().Contains(term) ||
                b.Author.ToLower().Contains(term) ||
                (b.Isbn != null && b.Isbn.ToLower().Contains(term)) ||
                (b.CourseCode != null && b.CourseCode.ToLower().Contains(term)));
        }

        // Category filter
        if (categoryId.HasValue && categoryId.Value > 0)
        {
            query = query.Where(b => b.CategoryId == categoryId.Value);
        }

        // Department filter
        if (departmentId.HasValue && departmentId.Value > 0)
        {
            query = query.Where(b => b.DepartmentId == departmentId.Value);
        }

        // Condition filter
        if (condition.HasValue)
        {
            query = query.Where(b => b.Condition == condition.Value);
        }

        // Exchange type filter
        if (exchangeType.HasValue)
        {
            query = query.Where(b => b.ExchangeType == exchangeType.Value);
        }

        // Price range
        if (minPrice.HasValue)
        {
            query = query.Where(b => b.Price >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(b => b.Price <= maxPrice.Value);
        }

        // Sorting
        query = sortBy?.ToLower() switch
        {
            "price_asc" => query.OrderBy(b => b.Price).ThenByDescending(b => b.CreatedAt),
            "price_desc" => query.OrderByDescending(b => b.Price).ThenByDescending(b => b.CreatedAt),
            "title" => query.OrderBy(b => b.Title),
            _ => query.OrderByDescending(b => b.CreatedAt)
        };

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<int> CountActiveListingsByUserAsync(int userId)
    {
        return await _dbContext.Books
            .CountAsync(b => b.OwnerId == userId && b.Status == BookStatus.Available);
    }

    public async Task<int> CountReservedListingsByUserAsync(int userId)
    {
        return await _dbContext.Books
            .CountAsync(b => b.OwnerId == userId && b.Status == BookStatus.Reserved);
    }
}

