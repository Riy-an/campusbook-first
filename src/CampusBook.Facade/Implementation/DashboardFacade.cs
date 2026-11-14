using CampusBook.DataAccess.Interface;
using CampusBook.Facade.Interface;
using CampusBook.Facade.ViewModels.Books;
using CampusBook.Facade.ViewModels.Dashboard;
using CampusBook.Facade.ViewModels.Requests;

namespace CampusBook.Facade.Implementation;

public class DashboardFacade : IDashboardFacade
{
    private readonly IUserRepository _userRepository;
    private readonly IBookRepository _bookRepository;
    private readonly IBookRequestRepository _requestRepository;
    private readonly IWishlistRepository _wishlistRepository;
    private readonly IWantedBookRepository _wantedBookRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IConversationRepository _conversationRepository;

    public DashboardFacade(
        IUserRepository userRepository,
        IBookRepository bookRepository,
        IBookRequestRepository requestRepository,
        IWishlistRepository wishlistRepository,
        IWantedBookRepository wantedBookRepository,
        INotificationRepository notificationRepository,
        IConversationRepository conversationRepository)
    {
        _userRepository = userRepository;
        _bookRepository = bookRepository;
        _requestRepository = requestRepository;
        _wishlistRepository = wishlistRepository;
        _wantedBookRepository = wantedBookRepository;
        _notificationRepository = notificationRepository;
        _conversationRepository = conversationRepository;
    }

    public async Task<DashboardViewModel> GetDashboardDataAsync(int userId)
    {
        var user = await _userRepository.GetUserWithProfileAsync(userId);
        var activeListingsCount = await _bookRepository.CountActiveListingsByUserAsync(userId);
        var reservedListingsCount = await _bookRepository.CountReservedListingsByUserAsync(userId);
        var pendingSentCount = await _requestRepository.CountPendingSentByUserAsync(userId);
        var pendingReceivedCount = await _requestRepository.CountPendingReceivedByUserAsync(userId);
        var completedCount = await _requestRepository.CountCompletedExchangesByUserAsync(userId);
        var wishlistCount = await _wishlistRepository.CountUserWishlistAsync(userId);
        var wantedCount = await _wantedBookRepository.CountActiveByUserAsync(userId);
        var unreadNotifications = await _notificationRepository.GetUnreadCountAsync(userId);
        var unreadMessages = await _conversationRepository.GetUnreadMessageCountAsync(userId);

        var myBooks = await _bookRepository.GetUserBooksAsync(userId);
        var incoming = await _requestRepository.GetReceivedRequestsByOwnerAsync(userId);
        var outgoing = await _requestRepository.GetSentRequestsByUserAsync(userId);

        return new DashboardViewModel
        {
            StudentName = user?.FullName ?? "Student",
            StudentId = user?.StudentId ?? string.Empty,
            Department = user?.StudentProfile?.Department ?? "Student",
            ActiveListingsCount = activeListingsCount,
            ReservedListingsCount = reservedListingsCount,
            PendingSentRequestsCount = pendingSentCount,
            PendingReceivedRequestsCount = pendingReceivedCount,
            CompletedExchangesCount = completedCount,
            WishlistCount = wishlistCount,
            WantedBooksCount = wantedCount,
            UnreadNotificationsCount = unreadNotifications,
            UnreadMessagesCount = unreadMessages,
            RecentListings = myBooks.Take(5).Select(b => new BookItemViewModel
            {
                BookId = b.BookId,
                Title = b.Title,
                Author = b.Author,
                CategoryName = b.Category?.Name ?? "General",
                DepartmentName = b.Department?.Name,
                Condition = b.Condition,
                ExchangeType = b.ExchangeType,
                Price = b.Price,
                ImageUrl = b.ImageUrl,
                OwnerId = b.OwnerId,
                Status = b.Status,
                CreatedAt = b.CreatedAt
            }),
            RecentIncomingRequests = incoming.Take(5).Select(r => new ReceivedRequestViewModel
            {
                RequestId = r.RequestId,
                BookId = r.BookId,
                BookTitle = r.Book.Title,
                RequesterId = r.RequesterId,
                RequesterName = r.Requester?.FullName ?? "Student",
                RequesterDepartment = r.Requester?.StudentProfile?.Department ?? "Student",
                OfferedBookTitle = r.OfferedBook?.Title,
                RequestMessage = r.RequestMessage,
                Status = r.Status,
                OwnerConfirmed = r.OwnerConfirmed,
                RequesterConfirmed = r.RequesterConfirmed,
                CreatedAt = r.CreatedAt
            }),
            RecentOutgoingRequests = outgoing.Take(5).Select(r => new SentRequestViewModel
            {
                RequestId = r.RequestId,
                BookId = r.BookId,
                BookTitle = r.Book.Title,
                BookAuthor = r.Book.Author,
                ExchangeType = r.Book.ExchangeType,
                Price = r.Book.Price,
                BookImageUrl = r.Book.ImageUrl,
                OwnerName = r.Book.Owner?.FullName ?? "Book Owner",
                OfferedBookTitle = r.OfferedBook?.Title,
                RequestMessage = r.RequestMessage,
                Status = r.Status,
                OwnerConfirmed = r.OwnerConfirmed,
                RequesterConfirmed = r.RequesterConfirmed,
                CreatedAt = r.CreatedAt
            })
        };
    }
}

