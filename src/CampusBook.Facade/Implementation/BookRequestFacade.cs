using CampusBook.DataAccess.Interface;
using CampusBook.Entities;
using CampusBook.Facade.Common;
using CampusBook.Facade.Interface;
using CampusBook.Facade.ViewModels.Requests;
using Microsoft.Extensions.Logging;

namespace CampusBook.Facade.Implementation;

public class BookRequestFacade : IBookRequestFacade
{
    private readonly IBookRequestRepository _requestRepository;
    private readonly IBookRepository _bookRepository;
    private readonly IUserRepository _userRepository;
    private readonly IBookFacade _bookFacade;
    private readonly ILogger<BookRequestFacade> _logger;

    public BookRequestFacade(
        IBookRequestRepository requestRepository,
        IBookRepository bookRepository,
        IUserRepository userRepository,
        IBookFacade bookFacade,
        ILogger<BookRequestFacade> logger)
    {
        _requestRepository = requestRepository;
        _bookRepository = bookRepository;
        _userRepository = userRepository;
        _bookFacade = bookFacade;
        _logger = logger;
    }

    public async Task<CreateRequestViewModel?> GetCreateRequestViewModelAsync(int bookId, int requesterId)
    {
        var book = await _bookRepository.GetBookDetailsAsync(bookId);
        if (book == null || book.OwnerId == requesterId || book.Status != BookStatus.Available)
        {
            return null;
        }

        var availableBooksDropdown = book.ExchangeType == ExchangeType.Exchange
            ? await _bookFacade.GetUserAvailableBooksDropdownAsync(requesterId)
            : Enumerable.Empty<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>();

        return new CreateRequestViewModel
        {
            BookId = book.BookId,
            BookTitle = book.Title,
            BookAuthor = book.Author,
            ExchangeType = book.ExchangeType,
            Price = book.Price,
            ImageUrl = book.ImageUrl,
            OwnerName = book.Owner?.FullName ?? "Book Owner",
            UserAvailableBooks = availableBooksDropdown
        };
    }

    public async Task<ServiceResult<int>> CreateRequestAsync(int requesterId, CreateRequestViewModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        // 1. Verify requester is email-confirmed
        var requester = await _userRepository.GetByIdAsync(requesterId);
        if (requester == null || !requester.IsEmailConfirmed)
        {
            return ServiceResult<int>.Fail("You must confirm your university email before you can request books.");
        }

        // 2. Verify requested book exists and is Available
        var book = await _bookRepository.GetByIdAsync(model.BookId);
        if (book == null)
        {
            return ServiceResult<int>.Fail("Requested book could not be found.");
        }

        if (book.Status != BookStatus.Available)
        {
            return ServiceResult<int>.Fail("This book is no longer available for request.");
        }

        // 3. Prevent requesting own book
        if (book.OwnerId == requesterId)
        {
            return ServiceResult<int>.Fail("You cannot request your own book listing.");
        }

        // 4. Duplicate active request check
        if (await _requestRepository.HasActiveRequestAsync(model.BookId, requesterId))
        {
            return ServiceResult<int>.Fail("You already have an active request pending or accepted for this book.");
        }

        // 5. Book-for-book exchange validation
        Book? offeredBook = null;
        if (book.ExchangeType == ExchangeType.Exchange)
        {
            if (!model.OfferedBookId.HasValue || model.OfferedBookId.Value <= 0)
            {
                return ServiceResult<int>.Fail("Please select one of your available books to offer in this exchange.");
            }

            if (model.OfferedBookId.Value == model.BookId)
            {
                return ServiceResult<int>.Fail("Offered book cannot be the same as the requested book.");
            }

            offeredBook = await _bookRepository.GetByIdAsync(model.OfferedBookId.Value);
            if (offeredBook == null || offeredBook.OwnerId != requesterId)
            {
                return ServiceResult<int>.Fail("The offered book does not belong to your account.");
            }

            if (offeredBook.Status != BookStatus.Available)
            {
                return ServiceResult<int>.Fail("The book you offered is currently unavailable or reserved.");
            }
        }

        var request = new BookRequest
        {
            BookId = model.BookId,
            RequesterId = requesterId,
            OfferedBookId = book.ExchangeType == ExchangeType.Exchange ? model.OfferedBookId : null,
            RequestMessage = string.IsNullOrWhiteSpace(model.RequestMessage) ? null : model.RequestMessage.Trim(),
            Status = RequestStatus.Pending,
            OwnerConfirmed = false,
            RequesterConfirmed = false,
            CreatedAt = DateTime.UtcNow
        };

        await _requestRepository.AddAsync(request);
        _logger.LogInformation("Student {RequesterId} submitted request {RequestId} for book {BookId}", requesterId, request.RequestId, book.BookId);

        return ServiceResult<int>.Ok(request.RequestId);
    }

    public async Task<IEnumerable<SentRequestViewModel>> GetSentRequestsAsync(int requesterId)
    {
        var requests = await _requestRepository.GetSentRequestsByUserAsync(requesterId);
        return requests.Select(r => new SentRequestViewModel
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
        });
    }

    public async Task<IEnumerable<ReceivedRequestViewModel>> GetReceivedRequestsAsync(int ownerId)
    {
        var requests = await _requestRepository.GetReceivedRequestsByOwnerAsync(ownerId);
        return requests.Select(r => new ReceivedRequestViewModel
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
        });
    }

    public async Task<RequestDetailsViewModel?> GetRequestDetailsAsync(int requestId, int currentUserId)
    {
        var request = await _requestRepository.GetRequestDetailsAsync(requestId);
        if (request == null) return null;

        var isOwner = request.Book.OwnerId == currentUserId;
        var isRequester = request.RequesterId == currentUserId;

        // Security check: only involved parties can view details
        if (!isOwner && !isRequester)
        {
            return null;
        }

        return new RequestDetailsViewModel
        {
            RequestId = request.RequestId,
            BookId = request.BookId,
            BookTitle = request.Book.Title,
            BookAuthor = request.Book.Author,
            ExchangeType = request.Book.ExchangeType,
            Price = request.Book.Price,
            BookImageUrl = request.Book.ImageUrl,
            OwnerId = request.Book.OwnerId,
            OwnerName = request.Book.Owner?.FullName ?? "Book Owner",
            OwnerDepartment = request.Book.Owner?.StudentProfile?.Department ?? "Student",
            OwnerEmail = request.Status == RequestStatus.Accepted || request.Status == RequestStatus.Completed ? request.Book.Owner?.Email : null,
            RequesterId = request.RequesterId,
            RequesterName = request.Requester.FullName,
            RequesterDepartment = request.Requester.StudentProfile?.Department ?? "Student",
            RequesterBatch = request.Requester.StudentProfile?.Batch ?? "Intake",
            RequesterEmail = request.Status == RequestStatus.Accepted || request.Status == RequestStatus.Completed ? request.Requester.Email : null,
            OfferedBookId = request.OfferedBookId,
            OfferedBookTitle = request.OfferedBook?.Title,
            OfferedBookAuthor = request.OfferedBook?.Author,
            OfferedBookImageUrl = request.OfferedBook?.ImageUrl,
            RequestMessage = request.RequestMessage,
            Status = request.Status,
            OwnerConfirmed = request.OwnerConfirmed,
            OwnerConfirmedAt = request.OwnerConfirmedAt,
            RequesterConfirmed = request.RequesterConfirmed,
            RequesterConfirmedAt = request.RequesterConfirmedAt,
            CreatedAt = request.CreatedAt,
            UpdatedAt = request.UpdatedAt,
            IsOwner = isOwner,
            IsRequester = isRequester
        };
    }

    public async Task<ServiceResult> CancelRequestAsync(int requestId, int requesterId)
    {
        var request = await _requestRepository.GetRequestDetailsAsync(requestId);
        if (request == null)
        {
            return ServiceResult.Fail("Request not found.");
        }

        if (request.RequesterId != requesterId)
        {
            return ServiceResult.Fail("You are not authorized to cancel this request.");
        }

        if (request.Status == RequestStatus.Completed)
        {
            return ServiceResult.Fail("A completed exchange cannot be cancelled.");
        }

        if (request.Status == RequestStatus.Cancelled || request.Status == RequestStatus.Rejected)
        {
            return ServiceResult.Fail("This request is already inactive.");
        }

        var wasAccepted = request.Status == RequestStatus.Accepted;

        request.Status = RequestStatus.Cancelled;
        request.UpdatedAt = DateTime.UtcNow;
        await _requestRepository.UpdateAsync(request);

        // If the request was Accepted, restore availability of the books
        if (wasAccepted)
        {
            var book = await _bookRepository.GetByIdAsync(request.BookId);
            if (book != null && book.Status == BookStatus.Reserved)
            {
                book.Status = BookStatus.Available;
                book.UpdatedAt = DateTime.UtcNow;
                await _bookRepository.UpdateAsync(book);
            }

            if (request.OfferedBookId.HasValue)
            {
                var offeredBook = await _bookRepository.GetByIdAsync(request.OfferedBookId.Value);
                if (offeredBook != null && offeredBook.Status == BookStatus.Reserved)
                {
                    offeredBook.Status = BookStatus.Available;
                    offeredBook.UpdatedAt = DateTime.UtcNow;
                    await _bookRepository.UpdateAsync(offeredBook);
                }
            }
        }

        _logger.LogInformation("Cancelled request {RequestId} by requester {RequesterId}", requestId, requesterId);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> AcceptRequestAsync(int requestId, int ownerId)
    {
        var request = await _requestRepository.GetRequestDetailsAsync(requestId);
        if (request == null)
        {
            return ServiceResult.Fail("Request not found.");
        }

        if (request.Book.OwnerId != ownerId)
        {
            return ServiceResult.Fail("Only the book owner may accept requests.");
        }

        if (request.Status != RequestStatus.Pending)
        {
            return ServiceResult.Fail($"Cannot accept request in '{request.Status}' status.");
        }

        // Concurrency & reservation check: Requested book must be Available
        var book = await _bookRepository.GetByIdAsync(request.BookId);
        if (book == null || book.Status != BookStatus.Available)
        {
            return ServiceResult.Fail("This book has already been reserved or is unavailable.");
        }

        // If exchange, offered book must also still be Available
        Book? offeredBook = null;
        if (request.OfferedBookId.HasValue)
        {
            offeredBook = await _bookRepository.GetByIdAsync(request.OfferedBookId.Value);
            if (offeredBook == null || offeredBook.Status != BookStatus.Available)
            {
                return ServiceResult.Fail("The offered book for this exchange is no longer available.");
            }
        }

        // Reserve requested book
        book.Status = BookStatus.Reserved;
        book.UpdatedAt = DateTime.UtcNow;
        await _bookRepository.UpdateAsync(book);

        // Reserve offered book if exchange
        if (offeredBook != null)
        {
            offeredBook.Status = BookStatus.Reserved;
            offeredBook.UpdatedAt = DateTime.UtcNow;
            await _bookRepository.UpdateAsync(offeredBook);
        }

        // Update request status to Accepted
        request.Status = RequestStatus.Accepted;
        request.UpdatedAt = DateTime.UtcNow;
        await _requestRepository.UpdateAsync(request);

        // Invalidate conflicting pending requests for the same book
        var otherPending = await _requestRepository.GetOtherPendingRequestsForBookAsync(request.BookId, requestId);
        foreach (var other in otherPending)
        {
            other.Status = RequestStatus.Rejected;
            other.UpdatedAt = DateTime.UtcNow;
            await _requestRepository.UpdateAsync(other);
        }

        _logger.LogInformation("Accepted request {RequestId} for book {BookId} by owner {OwnerId}", requestId, request.BookId, ownerId);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> RejectRequestAsync(int requestId, int ownerId)
    {
        var request = await _requestRepository.GetRequestDetailsAsync(requestId);
        if (request == null)
        {
            return ServiceResult.Fail("Request not found.");
        }

        if (request.Book.OwnerId != ownerId)
        {
            return ServiceResult.Fail("Only the book owner may reject requests.");
        }

        if (request.Status != RequestStatus.Pending)
        {
            return ServiceResult.Fail($"Cannot reject a request that is in '{request.Status}' status.");
        }

        request.Status = RequestStatus.Rejected;
        request.UpdatedAt = DateTime.UtcNow;
        await _requestRepository.UpdateAsync(request);

        _logger.LogInformation("Rejected request {RequestId} by owner {OwnerId}", requestId, ownerId);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> ConfirmHandoverAsync(int requestId, int currentUserId)
    {
        var request = await _requestRepository.GetRequestDetailsAsync(requestId);
        if (request == null)
        {
            return ServiceResult.Fail("Request not found.");
        }

        if (request.Status != RequestStatus.Accepted)
        {
            return ServiceResult.Fail("Handover can only be confirmed for an accepted exchange request.");
        }

        var isOwner = request.Book.OwnerId == currentUserId;
        var isRequester = request.RequesterId == currentUserId;

        if (!isOwner && !isRequester)
        {
            return ServiceResult.Fail("You are not authorized to confirm this handover.");
        }

        if (isOwner)
        {
            request.OwnerConfirmed = true;
            request.OwnerConfirmedAt = DateTime.UtcNow;
        }

        if (isRequester)
        {
            request.RequesterConfirmed = true;
            request.RequesterConfirmedAt = DateTime.UtcNow;
        }

        request.UpdatedAt = DateTime.UtcNow;

        // Both parties must confirm completion!
        if (request.OwnerConfirmed && request.RequesterConfirmed)
        {
            request.Status = RequestStatus.Completed;

            // Mark book unavailable (Completed)
            var book = await _bookRepository.GetByIdAsync(request.BookId);
            if (book != null)
            {
                book.Status = BookStatus.Completed;
                book.UpdatedAt = DateTime.UtcNow;
                await _bookRepository.UpdateAsync(book);
            }

            // Mark offered book unavailable if exchange
            if (request.OfferedBookId.HasValue)
            {
                var offeredBook = await _bookRepository.GetByIdAsync(request.OfferedBookId.Value);
                if (offeredBook != null)
                {
                    offeredBook.Status = BookStatus.Completed;
                    offeredBook.UpdatedAt = DateTime.UtcNow;
                    await _bookRepository.UpdateAsync(offeredBook);
                }
            }

            _logger.LogInformation("Exchange request {RequestId} successfully COMPLETED by mutual confirmation", requestId);
        }

        await _requestRepository.UpdateAsync(request);
        return ServiceResult.Ok();
    }
}

