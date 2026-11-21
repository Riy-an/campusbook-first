using CampusBook.Entities;
using CampusBook.Facade.Common;
using CampusBook.Facade.ViewModels.Admin;

namespace CampusBook.Facade.Interface;

public interface IAdminFacade
{
    // Dashboard
    Task<AdminDashboardViewModel> GetDashboardAsync();

    // Students
    Task<StudentListViewModel> GetStudentsPagedAsync(string? search, bool? isActive, StudentVerificationStatus? verificationStatus, string? sortBy, int pageIndex, int pageSize);
    Task<StudentDetailsViewModel?> GetStudentDetailsAsync(int userId);
    Task<ServiceResult> VerifyStudentAsync(int userId, StudentVerificationStatus status, string? notes, int adminId, string? ipAddress);
    Task<ServiceResult> SuspendStudentAsync(int userId, string reason, int adminId, string? ipAddress);
    Task<ServiceResult> ReactivateStudentAsync(int userId, int adminId, string? ipAddress);

    // Book Moderation
    Task<BookModerationListViewModel> GetBooksPagedAsync(string? search, int? categoryId, BookStatus? status, int pageIndex, int pageSize);
    Task<Book?> GetBookDetailsAsync(int bookId);
    Task<ServiceResult> ModerateBookAsync(int bookId, BookStatus status, string? reason, int adminId, string? ipAddress);

    // Classifications
    Task<CategoryManagementViewModel> GetCategoriesAsync();
    Task<CategoryEditViewModel?> GetCategoryForEditAsync(int id);
    Task<ServiceResult> SaveCategoryAsync(CategoryEditViewModel model, int adminId, string? ipAddress);
    Task<ServiceResult> ToggleCategoryStatusAsync(int id, int adminId, string? ipAddress);

    Task<DepartmentManagementViewModel> GetDepartmentsAsync();
    Task<DepartmentEditViewModel?> GetDepartmentForEditAsync(int id);
    Task<ServiceResult> SaveDepartmentAsync(DepartmentEditViewModel model, int adminId, string? ipAddress);

    Task<CourseManagementViewModel> GetCoursesAsync(int? departmentId);
    Task<CourseEditViewModel?> GetCourseForEditAsync(int id);
    Task<ServiceResult> SaveCourseAsync(CourseEditViewModel model, int adminId, string? ipAddress);
    Task<ServiceResult> ToggleCourseStatusAsync(int id, int adminId, string? ipAddress);

    // Requests & Exchanges
    Task<RequestMonitoringListViewModel> GetRequestsPagedAsync(string? search, RequestStatus? status, ExchangeType? type, int pageIndex, int pageSize);
    Task<BookRequest?> GetRequestDetailsAsync(int requestId);
    Task<ServiceResult> CancelRequestAsAdminAsync(int requestId, string reason, int adminId, string? ipAddress);

    // Reports
    Task<ReportListViewModel> GetReportsPagedAsync(ReportStatus? status, ReportTargetType? targetType, int pageIndex, int pageSize);
    Task<Report?> GetReportDetailsAsync(int reportId);
    Task<ServiceResult> ResolveReportAsync(int reportId, string resolutionNotes, int adminId, string? ipAddress);
    Task<ServiceResult> DismissReportAsync(int reportId, string resolutionNotes, int adminId, string? ipAddress);
    Task<ServiceResult> SubmitReportAsync(int reporterId, CreateReportViewModel model);

    // Reviews
    Task<ReviewModerationListViewModel> GetReviewsPagedAsync(bool? isHidden, int pageIndex, int pageSize);
    Task<ServiceResult> SetReviewVisibilityAsync(int reviewId, bool isHidden, string? reason, int adminId, string? ipAddress);
    Task<ServiceResult> SubmitReviewAsync(int reviewerId, CreateReviewViewModel model);

    // Analytics
    Task<AnalyticsDashboardViewModel> GetAnalyticsAsync();
    Task<byte[]> ExportStudentsCsvAsync();
    Task<byte[]> ExportTransactionsCsvAsync();
}
