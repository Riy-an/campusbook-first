using CampusBook.Entities;

namespace CampusBook.Facade.ViewModels.Admin;

public class AnalyticsDashboardViewModel
{
    public int TotalStudents { get; set; }
    public int VerifiedStudents { get; set; }
    public int SuspendedStudents { get; set; }

    public int TotalBooks { get; set; }
    public int CompletedSales { get; set; }
    public int CompletedExchanges { get; set; }

    public int TotalRequests { get; set; }
    public int AcceptedRequests { get; set; }
    public int RejectedRequests { get; set; }
    public int CancelledRequests { get; set; }

    public int TotalReports { get; set; }
    public int ResolvedReports { get; set; }

    public Dictionary<string, int> RegistrationTrends { get; set; } = new();
    public Dictionary<string, int> TransactionTrends { get; set; } = new();
    public Dictionary<string, int> CategoryDistribution { get; set; } = new();
}

public class AuditLogListViewModel
{
    public IEnumerable<AuditLog> Logs { get; set; } = new List<AuditLog>();
    public int TotalCount { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    public string? Action { get; set; }
    public int? AdminUserId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
