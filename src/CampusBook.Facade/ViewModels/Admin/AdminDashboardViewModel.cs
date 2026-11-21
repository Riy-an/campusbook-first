using CampusBook.Entities;

namespace CampusBook.Facade.ViewModels.Admin;

public class AdminDashboardViewModel
{
    // Student Metrics
    public int TotalStudents { get; set; }
    public int ActiveStudents { get; set; }
    public int SuspendedStudents { get; set; }
    public int VerifiedStudents { get; set; }
    public int PendingVerificationStudents { get; set; }
    public int RejectedVerificationStudents { get; set; }

    // Book Listing Metrics
    public int TotalBooks { get; set; }
    public int AvailableBooks { get; set; }
    public int ReservedBooks { get; set; }
    public int CompletedBooks { get; set; }
    public int InactiveBooks { get; set; }

    // Request & Transaction Metrics
    public int TotalRequests { get; set; }
    public int PendingRequests { get; set; }
    public int AcceptedRequests { get; set; }
    public int RejectedRequests { get; set; }
    public int CancelledRequests { get; set; }
    public int CompletedRequests { get; set; }
    public int CompletedSales { get; set; }
    public int CompletedExchanges { get; set; }

    // Report Metrics
    public int TotalReports { get; set; }
    public int PendingReports { get; set; }
    public int ResolvedReports { get; set; }

    // Recent Activity
    public IEnumerable<User> RecentStudents { get; set; } = new List<User>();
    public IEnumerable<Book> RecentBooks { get; set; } = new List<Book>();
    public IEnumerable<BookRequest> RecentRequests { get; set; } = new List<BookRequest>();
    public IEnumerable<Report> RecentReports { get; set; } = new List<Report>();

    // Visualizations / Breakdown
    public Dictionary<string, int> CategoryDistribution { get; set; } = new();
}
