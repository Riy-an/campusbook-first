using CampusBook.DataAccess.Interface;
using CampusBook.Entities;
using Microsoft.EntityFrameworkCore;

namespace CampusBook.DataAccess.Implementation;

public class BookRequestRepository : Repository<BookRequest>, IBookRequestRepository
{
    public BookRequestRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<BookRequest?> GetRequestDetailsAsync(int requestId)
    {
        return await _dbContext.BookRequests
            .Include(r => r.Book)
                .ThenInclude(b => b.Owner)
                    .ThenInclude(o => o.StudentProfile)
            .Include(r => r.Requester)
                .ThenInclude(u => u.StudentProfile)
            .Include(r => r.OfferedBook)
            .FirstOrDefaultAsync(r => r.RequestId == requestId);
    }

    public async Task<IEnumerable<BookRequest>> GetSentRequestsByUserAsync(int requesterId)
    {
        return await _dbContext.BookRequests
            .Include(r => r.Book)
                .ThenInclude(b => b.Owner)
            .Include(r => r.OfferedBook)
            .Where(r => r.RequesterId == requesterId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<BookRequest>> GetReceivedRequestsByOwnerAsync(int ownerId)
    {
        return await _dbContext.BookRequests
            .Include(r => r.Book)
            .Include(r => r.Requester)
                .ThenInclude(u => u.StudentProfile)
            .Include(r => r.OfferedBook)
            .Where(r => r.Book.OwnerId == ownerId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> HasActiveRequestAsync(int bookId, int requesterId)
    {
        return await _dbContext.BookRequests
            .AnyAsync(r => r.BookId == bookId &&
                           r.RequesterId == requesterId &&
                           (r.Status == RequestStatus.Pending || r.Status == RequestStatus.Accepted));
    }

    public async Task<IEnumerable<BookRequest>> GetOtherPendingRequestsForBookAsync(int bookId, int excludeRequestId)
    {
        return await _dbContext.BookRequests
            .Where(r => r.BookId == bookId &&
                        r.RequestId != excludeRequestId &&
                        r.Status == RequestStatus.Pending)
            .ToListAsync();
    }

    public async Task<int> CountPendingSentByUserAsync(int userId)
    {
        return await _dbContext.BookRequests
            .CountAsync(r => r.RequesterId == userId && r.Status == RequestStatus.Pending);
    }

    public async Task<int> CountPendingReceivedByUserAsync(int userId)
    {
        return await _dbContext.BookRequests
            .CountAsync(r => r.Book.OwnerId == userId && r.Status == RequestStatus.Pending);
    }

    public async Task<int> CountCompletedExchangesByUserAsync(int userId)
    {
        return await _dbContext.BookRequests
            .CountAsync(r => (r.RequesterId == userId || r.Book.OwnerId == userId) &&
                             r.Status == RequestStatus.Completed);
    }
}

