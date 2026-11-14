using CampusBook.Entities;
using CampusBook.Facade.ViewModels.Requests;

namespace CampusBook.Facade.ViewModels.History;

public enum HistoryRoleFilter
{
    All = 0,
    Sold = 1,
    Purchased = 2,
    Exchanged = 3
}

public class ExchangeHistoryItemViewModel
{
    public int RequestId { get; set; }
    public int BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public string BookAuthor { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? BookImageUrl { get; set; }
    public ExchangeType ExchangeType { get; set; }
    public decimal Price { get; set; }

    // User's role in this transaction
    public bool WasOwner { get; set; } // true = Seller/Owner, false = Buyer/Requester
    public string TransactionRoleBadge => ExchangeType == ExchangeType.Exchange 
        ? "Book Exchange" 
        : (WasOwner ? "Sold / Provider" : "Purchased / Acquirer");

    // Other participant details
    public string OtherPartyName { get; set; } = string.Empty;
    public string OtherPartyDepartment { get; set; } = string.Empty;
    public string? OtherPartyBatch { get; set; }

    // Offered book snapshot if exchange
    public ExchangeOfferDetailsViewModel? OfferedBookDetails { get; set; }

    public DateTime CompletedAt { get; set; }
    public RequestStatus FinalStatus { get; set; } = RequestStatus.Completed;
}

public class ExchangeHistoryPageViewModel
{
    public IEnumerable<ExchangeHistoryItemViewModel> Transactions { get; set; } = new List<ExchangeHistoryItemViewModel>();
    public HistoryRoleFilter Filter { get; set; } = HistoryRoleFilter.All;
    public int TotalSold { get; set; }
    public int TotalPurchased { get; set; }
    public int TotalExchanged { get; set; }
}
