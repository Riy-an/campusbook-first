using CampusBook.DataAccess.Interface;
using CampusBook.Entities;
using CampusBook.Facade.Common;
using CampusBook.Facade.Interface;
using CampusBook.Facade.Services;
using CampusBook.Facade.ViewModels.Requests;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;

namespace CampusBook.Facade.Implementation;

public class BookRequestFacade : IBookRequestFacade
{
    private readonly IBookRequestRepository _requestRepository;
    private readonly IBookRepository _bookRepository;
    private readonly IUserRepository _userRepository;
    private readonly IBookFacade _bookFacade;
    private readonly ILookupRepository _lookupRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly INotificationRepository _notificationRepository;
    private readonly IConversationRepository _conversationRepository;
    private readonly ILogger<BookRequestFacade> _logger;

    public BookRequestFacade(
        IBookRequestRepository requestRepository,
        IBookRepository bookRepository,
        IUserRepository userRepository,
        IBookFacade bookFacade,
        ILookupRepository lookupRepository,
        IFileStorageService fileStorageService,
        INotificationRepository notificationRepository,
        IConversationRepository conversationRepository,
        ILogger<BookRequestFacade> logger)
    {
        _requestRepository = requestRepository;
        _bookRepository = bookRepository;
        _userRepository = userRepository;
        _bookFacade = bookFacade;
        _lookupRepository = lookupRepository;
        _fileStorageService = fileStorageService;
        _notificationRepository = notificationRepository;
        _conversationRepository = conversationRepository;
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
            : Enumerable.Empty<SelectListItem>();

        var categories = await _lookupRepository.GetAllCategoriesAsync();
        var departments = await _lookupRepository.GetAllDepartmentsAsync();

        return new CreateRequestViewModel
        {
            BookId = book.BookId,
            BookTitle = book.Title,
            BookAuthor = book.Author,
            ExchangeType = book.ExchangeType,
            Price = book.Price,
            ImageUrl = book.ImageUrl,
            OwnerName = book.Owner?.FullName ?? "Book Owner",
            UserAvailableBooks = availableBooksDropdown,
            Categories = categories.Select(c => new SelectListItem { Value = c.CategoryId.ToString(), Text = c.Name }),
            Departments = departments.Select(d => new SelectListItem { Value = d.DepartmentId.ToString(), Text = $"{d.Code} - {d.Name}" })
        };
    }

    public async Task<ServiceResult<int>> CreateRequestAsync(int requesterId, CreateRequestViewModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        // 1. Verify requester exists and is active
        var requester = await _userRepository.GetByIdAsync(requesterId);
        if (requester == null || !requester.IsActive)
        {
            return ServiceResult<int>.Fail("Student account not found or inactive.");
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

        // 5. Book-for-book exchange validation & ExchangeOffer persistence
        ExchangeOffer? exchangeOffer = null;
        if (book.ExchangeType == ExchangeType.Exchange)
        {
            if (string.IsNullOrWhiteSpace(model.OfferTitle))
            {
                return ServiceResult<int>.Fail("Offered book title is required for exchange requests.");
            }

            if (string.IsNullOrWhiteSpace(model.OfferAuthor))
            {
                return ServiceResult<int>.Fail("Author of the offered book is required.");
            }

            if (!model.OfferCategoryId.HasValue || model.OfferCategoryId.Value <= 0)
            {
                return ServiceResult<int>.Fail("Subject category for the offered book is required.");
            }

            if (string.IsNullOrWhiteSpace(model.OfferDescription))
            {
                return ServiceResult<int>.Fail("Description of the offered book is required.");
            }

            string? offerImageUrl = null;
            if (model.OfferImage != null && model.OfferImage.Length > 0)
            {
                if (!_fileStorageService.IsValidImage(model.OfferImage, out var imgError))
                {
                    return ServiceResult<int>.Fail($"Offered book image error: {imgError}");
                }
                offerImageUrl = await _fileStorageService.SaveFileAsync(model.OfferImage, "uploads/books");
            }

            // If user selected one of their existing listings to link
            if (model.OfferedBookListingId.HasValue && model.OfferedBookListingId.Value > 0)
            {
                var offeredListing = await _bookRepository.GetByIdAsync(model.OfferedBookListingId.Value);
                if (offeredListing != null && offeredListing.OwnerId == requesterId && offeredListing.Status == BookStatus.Available)
                {
                    if (string.IsNullOrEmpty(offerImageUrl) && !string.IsNullOrEmpty(offeredListing.ImageUrl))
                    {
                        offerImageUrl = offeredListing.ImageUrl;
                    }
                }
            }

            exchangeOffer = new ExchangeOffer
            {
                Title = model.OfferTitle.Trim(),
                Author = model.OfferAuthor.Trim(),
                Isbn = string.IsNullOrWhiteSpace(model.OfferIsbn) ? null : model.OfferIsbn.Trim(),
                Edition = string.IsNullOrWhiteSpace(model.OfferEdition) ? null : model.OfferEdition.Trim(),
                PublicationYear = string.IsNullOrWhiteSpace(model.OfferPublicationYear) ? null : model.OfferPublicationYear.Trim(),
                CategoryId = model.OfferCategoryId.Value,
                DepartmentId = model.OfferDepartmentId > 0 ? model.OfferDepartmentId : null,
                CourseCode = string.IsNullOrWhiteSpace(model.OfferCourseCode) ? null : model.OfferCourseCode.Trim(),
                Condition = model.OfferCondition,
                Description = model.OfferDescription.Trim(),
                Notes = string.IsNullOrWhiteSpace(model.OfferNotes) ? null : model.OfferNotes.Trim(),
                ImageUrl = offerImageUrl,
                OfferedBookListingId = model.OfferedBookListingId > 0 ? model.OfferedBookListingId : null,
                CreatedAt = DateTime.UtcNow
            };
        }

        var request = new BookRequest
        {
            BookId = model.BookId,
            RequesterId = requesterId,
            OfferedBookId = model.OfferedBookListingId > 0 ? model.OfferedBookListingId : null,
            ExchangeOffer = exchangeOffer,
            RequestMessage = string.IsNullOrWhiteSpace(model.RequestMessage) ? null : model.RequestMessage.Trim(),
            Status = RequestStatus.Pending,
            OwnerConfirmed = false,
            RequesterConfirmed = false,
            CreatedAt = DateTime.UtcNow
        };

        await _requestRepository.AddAsync(request);
        _logger.LogInformation("Student {RequesterId} submitted request {RequestId} for book {BookId}", requesterId, request.RequestId, book.BookId);

        // Notify book owner of incoming request
        await _notificationRepository.AddAsync(new Notification
        {
            UserId = book.OwnerId,
            Title = "New Book Request Received",
            Message = $"{requester.FullName} submitted a request for your book \"{book.Title}\" ({(book.ExchangeType == ExchangeType.Exchange ? "Exchange Offer" : "Sale Request")}).",
            Type = NotificationType.RequestReceived,
            RelatedEntityType = "BookRequest",
            RelatedEntityId = request.RequestId,
            NavigationUrl = $"/requests/details/{request.RequestId}",
            CreatedAt = DateTime.UtcNow
        });

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
            OfferedBookTitle = r.ExchangeOffer?.Title ?? r.OfferedBook?.Title,
            RequestMessage = r.RequestMessage,
            Status = r.Status,
            OwnerConfirmed = r.OwnerConfirmed,
            RequesterConfirmed = r.RequesterConfirmed,
            CreatedAt = r.CreatedAt,
            ConversationId = r.Conversation?.ConversationId
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
            ExchangeType = r.Book.ExchangeType,
            OfferedBookTitle = r.ExchangeOffer?.Title ?? r.OfferedBook?.Title,
            RequestMessage = r.RequestMessage,
            Status = r.Status,
            OwnerConfirmed = r.OwnerConfirmed,
            RequesterConfirmed = r.RequesterConfirmed,
            CreatedAt = r.CreatedAt,
            ConversationId = r.Conversation?.ConversationId
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

        ExchangeOfferDetailsViewModel? offerDetails = null;
        if (request.ExchangeOffer != null)
        {
            offerDetails = new ExchangeOfferDetailsViewModel
            {
                ExchangeOfferId = request.ExchangeOffer.ExchangeOfferId,
                Title = request.ExchangeOffer.Title,
                Author = request.ExchangeOffer.Author,
                Isbn = request.ExchangeOffer.Isbn,
                Edition = request.ExchangeOffer.Edition,
                PublicationYear = request.ExchangeOffer.PublicationYear,
                CategoryName = request.ExchangeOffer.Category?.Name ?? "General",
                DepartmentName = request.ExchangeOffer.Department?.Name,
                CourseCode = request.ExchangeOffer.CourseCode,
                Condition = request.ExchangeOffer.Condition,
                Description = request.ExchangeOffer.Description,
                Notes = request.ExchangeOffer.Notes,
                ImageUrl = request.ExchangeOffer.ImageUrl,
                OfferedBookListingId = request.ExchangeOffer.OfferedBookListingId
            };
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
            ExchangeOffer = offerDetails,
            RequestMessage = request.RequestMessage,
            Status = request.Status,
            OwnerConfirmed = request.OwnerConfirmed,
            OwnerConfirmedAt = request.OwnerConfirmedAt,
            RequesterConfirmed = request.RequesterConfirmed,
            RequesterConfirmedAt = request.RequesterConfirmedAt,
            CreatedAt = request.CreatedAt,
            UpdatedAt = request.UpdatedAt,
            ConversationId = request.Conversation?.ConversationId,
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

            var offeredListingId = request.ExchangeOffer?.OfferedBookListingId ?? request.OfferedBookId;
            if (offeredListingId.HasValue)
            {
                var offeredBook = await _bookRepository.GetByIdAsync(offeredListingId.Value);
                if (offeredBook != null && offeredBook.Status == BookStatus.Reserved)
                {
                    offeredBook.Status = BookStatus.Available;
                    offeredBook.UpdatedAt = DateTime.UtcNow;
                    await _bookRepository.UpdateAsync(offeredBook);
                }
            }

            // Notify owner of cancellation
            await _notificationRepository.AddAsync(new Notification
            {
                UserId = request.Book.OwnerId,
                Title = "Exchange Request Cancelled",
                Message = $"{request.Requester.FullName} cancelled their request for \"{request.Book.Title}\". Your book is now available again.",
                Type = NotificationType.RequestCancelled,
                RelatedEntityType = "BookRequest",
                RelatedEntityId = request.RequestId,
                NavigationUrl = $"/requests/details/{request.RequestId}",
                CreatedAt = DateTime.UtcNow
            });
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

        // If exchange with linked listing, offered book must also still be Available
        Book? offeredBook = null;
        var offeredListingId = request.ExchangeOffer?.OfferedBookListingId ?? request.OfferedBookId;
        if (offeredListingId.HasValue)
        {
            offeredBook = await _bookRepository.GetByIdAsync(offeredListingId.Value);
            if (offeredBook != null && offeredBook.Status != BookStatus.Available)
            {
                return ServiceResult.Fail("The offered book listing for this exchange is no longer available.");
            }
        }

        // Reserve requested book
        book.Status = BookStatus.Reserved;
        book.UpdatedAt = DateTime.UtcNow;
        await _bookRepository.UpdateAsync(book);

        // Reserve offered book listing if linked
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

        // Initialize Conversation for messaging
        var existingConv = await _conversationRepository.GetByRequestIdAsync(requestId);
        if (existingConv == null)
        {
            await _conversationRepository.AddAsync(new Conversation
            {
                RequestId = requestId,
                CreatedAt = DateTime.UtcNow,
                LastActivityAt = DateTime.UtcNow
            });
        }

        // Invalidate conflicting pending requests for the same book
        var otherPending = await _requestRepository.GetOtherPendingRequestsForBookAsync(request.BookId, requestId);
        foreach (var other in otherPending)
        {
            other.Status = RequestStatus.Rejected;
            other.UpdatedAt = DateTime.UtcNow;
            await _requestRepository.UpdateAsync(other);

            // Notify competing requesters that the book is now reserved
            await _notificationRepository.AddAsync(new Notification
            {
                UserId = other.RequesterId,
                Title = "Book No Longer Available",
                Message = $"The book \"{book.Title}\" was reserved for another student's request.",
                Type = NotificationType.BookUnavailable,
                RelatedEntityType = "Book",
                RelatedEntityId = book.BookId,
                NavigationUrl = $"/books/details/{book.BookId}",
                CreatedAt = DateTime.UtcNow
            });
        }

        // Notify accepted requester
        await _notificationRepository.AddAsync(new Notification
        {
            UserId = request.RequesterId,
            Title = "Request Accepted! 🎉",
            Message = $"Great news! Your request for \"{book.Title}\" was accepted by {request.Book.Owner.FullName}. You can now send messages to coordinate the handover.",
            Type = NotificationType.RequestAccepted,
            RelatedEntityType = "BookRequest",
            RelatedEntityId = request.RequestId,
            NavigationUrl = $"/requests/details/{request.RequestId}",
            CreatedAt = DateTime.UtcNow
        });

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

        // Notify requester
        await _notificationRepository.AddAsync(new Notification
        {
            UserId = request.RequesterId,
            Title = "Request Declined",
            Message = $"Your request for \"{request.Book.Title}\" was declined by the owner.",
            Type = NotificationType.RequestRejected,
            RelatedEntityType = "BookRequest",
            RelatedEntityId = request.RequestId,
            NavigationUrl = $"/requests/details/{request.RequestId}",
            CreatedAt = DateTime.UtcNow
        });

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

            // Mark offered book listing unavailable if linked
            var offeredListingId = request.ExchangeOffer?.OfferedBookListingId ?? request.OfferedBookId;
            if (offeredListingId.HasValue)
            {
                var offeredBook = await _bookRepository.GetByIdAsync(offeredListingId.Value);
                if (offeredBook != null)
                {
                    offeredBook.Status = BookStatus.Completed;
                    offeredBook.UpdatedAt = DateTime.UtcNow;
                    await _bookRepository.UpdateAsync(offeredBook);
                }
            }

            // Notify both parties of successful exchange completion
            await _notificationRepository.AddAsync(new Notification
            {
                UserId = request.Book.OwnerId,
                Title = "Exchange Completed! 🤝",
                Message = $"The handover of \"{request.Book.Title}\" has been confirmed by both parties. The exchange is now complete!",
                Type = NotificationType.ExchangeCompleted,
                RelatedEntityType = "BookRequest",
                RelatedEntityId = request.RequestId,
                NavigationUrl = $"/requests/details/{request.RequestId}",
                CreatedAt = DateTime.UtcNow
            });

            await _notificationRepository.AddAsync(new Notification
            {
                UserId = request.RequesterId,
                Title = "Exchange Completed! 🤝",
                Message = $"The handover of \"{request.Book.Title}\" has been confirmed by both parties. Enjoy your book!",
                Type = NotificationType.ExchangeCompleted,
                RelatedEntityType = "BookRequest",
                RelatedEntityId = request.RequestId,
                NavigationUrl = $"/requests/details/{request.RequestId}",
                CreatedAt = DateTime.UtcNow
            });

            _logger.LogInformation("Exchange request {RequestId} successfully COMPLETED by mutual confirmation", requestId);
        }

        await _requestRepository.UpdateAsync(request);
        return ServiceResult.Ok();
    }
}
