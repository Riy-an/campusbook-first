using CampusBook.Entities;

namespace CampusBook.DataAccess.Interface;

public interface IBookRequestRepository : IRepository<BookRequest>
{
    Task<BookRequest?> GetRequestDetailsAsync(int requestId);
    Task<IEnumerable<BookRequest>> GetSentRequestsByUserAsync(int requesterId);
    Task<IEnumerable<BookRequest>> GetReceivedRequestsByOwnerAsync(int ownerId);
    Task<bool> HasActiveRequestAsync(int bookId, int requesterId);
    Task<IEnumerable<BookRequest>> GetOtherPendingRequestsForBookAsync(int bookId, int excludeRequestId);
    Task<int> CountPendingSentByUserAsync(int userId);
    Task<int> CountPendingReceivedByUserAsync(int userId);
    Task<int> CountCompletedExchangesByUserAsync(int userId);
    Task<IEnumerable<BookRequest>> GetCompletedTransactionsByUserAsync(int userId);
}

