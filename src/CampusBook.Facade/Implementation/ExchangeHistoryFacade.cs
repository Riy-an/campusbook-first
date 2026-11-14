using CampusBook.DataAccess.Interface;
using CampusBook.Entities;
using CampusBook.Facade.Interface;
using CampusBook.Facade.ViewModels.History;
using CampusBook.Facade.ViewModels.Requests;
using Microsoft.Extensions.Logging;

namespace CampusBook.Facade.Implementation;

public class ExchangeHistoryFacade : IExchangeHistoryFacade
{
    private readonly IBookRequestRepository _requestRepository;
    private readonly ILogger<ExchangeHistoryFacade> _logger;

    public ExchangeHistoryFacade(
        IBookRequestRepository requestRepository,
        ILogger<ExchangeHistoryFacade> logger)
    {
        _requestRepository = requestRepository;
        _logger = logger;
    }

    public async Task<ExchangeHistoryPageViewModel> GetUserExchangeHistoryAsync(int userId, HistoryRoleFilter filter = HistoryRoleFilter.All)
    {
        var completedRequests = await _requestRepository.GetCompletedTransactionsByUserAsync(userId);

        var allItems = completedRequests.Select(r =>
        {
            var isOwner = r.Book.OwnerId == userId;
            var otherUser = isOwner ? r.Requester : r.Book.Owner;

            ExchangeOfferDetailsViewModel? offerDetails = null;
            if (r.ExchangeOffer != null)
            {
                offerDetails = new ExchangeOfferDetailsViewModel
                {
                    ExchangeOfferId = r.ExchangeOffer.ExchangeOfferId,
                    Title = r.ExchangeOffer.Title,
                    Author = r.ExchangeOffer.Author,
                    Isbn = r.ExchangeOffer.Isbn,
                    Edition = r.ExchangeOffer.Edition,
                    PublicationYear = r.ExchangeOffer.PublicationYear,
                    CategoryName = r.ExchangeOffer.Category?.Name ?? "General",
                    DepartmentName = r.ExchangeOffer.Department?.Name,
                    CourseCode = r.ExchangeOffer.CourseCode,
                    Condition = r.ExchangeOffer.Condition,
                    Description = r.ExchangeOffer.Description,
                    Notes = r.ExchangeOffer.Notes,
                    ImageUrl = r.ExchangeOffer.ImageUrl
                };
            }

            return new ExchangeHistoryItemViewModel
            {
                RequestId = r.RequestId,
                BookId = r.BookId,
                BookTitle = r.Book.Title,
                BookAuthor = r.Book.Author,
                CategoryName = r.Book.Category?.Name ?? "General",
                BookImageUrl = r.Book.ImageUrl,
                ExchangeType = r.Book.ExchangeType,
                Price = r.Book.Price,
                WasOwner = isOwner,
                OtherPartyName = otherUser?.FullName ?? "Student",
                OtherPartyDepartment = otherUser?.StudentProfile?.Department ?? "Student",
                OtherPartyBatch = otherUser?.StudentProfile?.Batch,
                OfferedBookDetails = offerDetails,
                CompletedAt = r.UpdatedAt ?? r.CreatedAt,
                FinalStatus = r.Status
            };
        }).ToList();

        // Calculate summary counters
        var totalSold = allItems.Count(i => i.WasOwner && i.ExchangeType == ExchangeType.Sale);
        var totalPurchased = allItems.Count(i => !i.WasOwner && i.ExchangeType == ExchangeType.Sale);
        var totalExchanged = allItems.Count(i => i.ExchangeType == ExchangeType.Exchange);

        // Apply selected filter
        var filteredItems = filter switch
        {
            HistoryRoleFilter.Sold => allItems.Where(i => i.WasOwner && i.ExchangeType == ExchangeType.Sale),
            HistoryRoleFilter.Purchased => allItems.Where(i => !i.WasOwner && i.ExchangeType == ExchangeType.Sale),
            HistoryRoleFilter.Exchanged => allItems.Where(i => i.ExchangeType == ExchangeType.Exchange),
            _ => allItems
        };

        return new ExchangeHistoryPageViewModel
        {
            Transactions = filteredItems.OrderByDescending(i => i.CompletedAt),
            Filter = filter,
            TotalSold = totalSold,
            TotalPurchased = totalPurchased,
            TotalExchanged = totalExchanged
        };
    }
}
