using CampusBook.Facade.ViewModels.Books;
using CampusBook.Facade.ViewModels.Requests;

namespace CampusBook.Facade.ViewModels.Dashboard;

public class DashboardViewModel
{
    public string StudentName { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;

    public int ActiveListingsCount { get; set; }
    public int ReservedListingsCount { get; set; }
    public int PendingReceivedRequestsCount { get; set; }
    public int PendingSentRequestsCount { get; set; }
    public int CompletedExchangesCount { get; set; }

    public IEnumerable<BookItemViewModel> RecentListings { get; set; } = new List<BookItemViewModel>();
    public IEnumerable<ReceivedRequestViewModel> RecentIncomingRequests { get; set; } = new List<ReceivedRequestViewModel>();
    public IEnumerable<SentRequestViewModel> RecentOutgoingRequests { get; set; } = new List<SentRequestViewModel>();
}

