using CampusBook.Entities;

namespace CampusBook.Facade.ViewModels.Admin;

public class RequestMonitoringListViewModel
{
    public IEnumerable<BookRequest> Requests { get; set; } = new List<BookRequest>();
    public int TotalCount { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 15;
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    public string? SearchTerm { get; set; }
    public RequestStatus? Status { get; set; }
    public ExchangeType? Type { get; set; }
}

public class RequestInterventionViewModel
{
    public int RequestId { get; set; }
    public string Reason { get; set; } = string.Empty;
}
