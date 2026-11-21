using CampusBook.Entities;

namespace CampusBook.DataAccess.Interface;

public interface IAdminRepository
{
    // Dashboard
    Task<(int TotalStudents, int ActiveStudents, int SuspendedStudents, int VerifiedStudents, int PendingVerificationStudents, int RejectedVerificationStudents)> GetStudentMetricCountsAsync();
    Task<(int TotalBooks, int AvailableBooks, int ReservedBooks, int CompletedBooks, int InactiveBooks)> GetBookMetricCountsAsync();
    Task<(int TotalRequests, int PendingRequests, int AcceptedRequests, int RejectedRequests, int CancelledRequests, int CompletedRequests)> GetRequestMetricCountsAsync();
    Task<(int CompletedSales, int CompletedExchanges)> GetCompletedTransactionCountsAsync();
    Task<(int TotalReports, int PendingReports, int ResolvedReports)> GetReportMetricCountsAsync();
    Task<IEnumerable<User>> GetRecentStudentsAsync(int count);
    Task<IEnumerable<Book>> GetRecentBooksAsync(int count);
    Task<IEnumerable<BookRequest>> GetRecentRequestsAsync(int count);
    Task<IEnumerable<Report>> GetRecentReportsAsync(int count);
    Task<Dictionary<string, int>> GetCategoryDistributionAsync();

    // Students
    Task<(IEnumerable<User> Items, int TotalCount)> GetStudentsPagedAsync(
        string? search,
        bool? isActive,
        StudentVerificationStatus? verificationStatus,
        string? sortBy,
        int pageIndex,
        int pageSize);
    Task<User?> GetStudentDetailsAsync(int userId);
    Task<bool> VerifyStudentAsync(int userId, StudentVerificationStatus status, string? notes, int adminId);
    Task<bool> SuspendStudentAsync(int userId, string reason, int adminId);
    Task<bool> ReactivateStudentAsync(int userId, int adminId);

    // Book Moderation
    Task<(IEnumerable<Book> Items, int TotalCount)> GetBooksPagedAsync(
        string? search,
        int? categoryId,
        BookStatus? status,
        int pageIndex,
        int pageSize);
    Task<Book?> GetBookDetailsAsync(int bookId);
    Task<bool> ModerateBookAsync(int bookId, BookStatus status, string? reason, int adminId);

    // Classifications
    Task<IEnumerable<(BookCategory Category, int BookCount)>> GetCategoriesWithCountsAsync();
    Task<IEnumerable<(Department Department, int CourseCount, int BookCount)>> GetDepartmentsWithCountsAsync();
    Task<IEnumerable<Course>> GetCoursesAsync(int? departmentId);
    Task<bool> CategoryExistsAsync(string name, int? excludeId = null);
    Task<bool> CourseExistsAsync(int departmentId, string courseCode, int? excludeId = null);

    // Requests & Exchanges
    Task<(IEnumerable<BookRequest> Items, int TotalCount)> GetRequestsPagedAsync(
        string? search,
        RequestStatus? status,
        ExchangeType? type,
        int pageIndex,
        int pageSize);
    Task<BookRequest?> GetRequestDetailsAsync(int requestId);
    Task<bool> CancelRequestAsAdminAsync(int requestId, string reason, int adminId);

    // Reports
    Task<(IEnumerable<Report> Items, int TotalCount)> GetReportsPagedAsync(
        ReportStatus? status,
        ReportTargetType? targetType,
        int pageIndex,
        int pageSize);
    Task<Report?> GetReportDetailsAsync(int reportId);
    Task<bool> UpdateReportStatusAsync(int reportId, ReportStatus status, string resolutionNotes, int adminId);
    Task AddReportAsync(Report report);

    // Reviews
    Task<(IEnumerable<ExchangeReview> Items, int TotalCount)> GetReviewsPagedAsync(
        bool? isHidden,
        int pageIndex,
        int pageSize);
    Task<bool> SetReviewVisibilityAsync(int reviewId, bool isHidden, string? reason, int adminId);
    Task AddReviewAsync(ExchangeReview review);

    // Analytics
    Task<Dictionary<string, int>> GetRegistrationTrendsAsync(int months);
    Task<Dictionary<string, int>> GetTransactionTrendsAsync(int months);
}
