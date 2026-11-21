using System.Text;
using CampusBook.DataAccess;
using CampusBook.DataAccess.Interface;
using CampusBook.Entities;
using CampusBook.Facade.Common;
using CampusBook.Facade.Interface;
using CampusBook.Facade.Services;
using CampusBook.Facade.ViewModels.Admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CampusBook.Facade.Implementation;

public class AdminFacade : IAdminFacade
{
    private readonly IAdminRepository _adminRepo;
    private readonly IAuditLogService _auditLogService;
    private readonly IPermissionService _permissionService;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<AdminFacade> _logger;

    public AdminFacade(
        IAdminRepository adminRepo,
        IAuditLogService auditLogService,
        IPermissionService permissionService,
        ApplicationDbContext dbContext,
        ILogger<AdminFacade> logger)
    {
        _adminRepo = adminRepo;
        _auditLogService = auditLogService;
        _permissionService = permissionService;
        _dbContext = dbContext;
        _logger = logger;
    }

    // --- Dashboard ---
    public async Task<AdminDashboardViewModel> GetDashboardAsync()
    {
        var (totalStudents, activeStudents, suspendedStudents, verifiedStudents, pendingStudents, rejectedStudents) =
            await _adminRepo.GetStudentMetricCountsAsync();

        var (totalBooks, availableBooks, reservedBooks, completedBooks, inactiveBooks) =
            await _adminRepo.GetBookMetricCountsAsync();

        var (totalRequests, pendingRequests, acceptedRequests, rejectedRequests, cancelledRequests, completedRequests) =
            await _adminRepo.GetRequestMetricCountsAsync();

        var (completedSales, completedExchanges) =
            await _adminRepo.GetCompletedTransactionCountsAsync();

        var (totalReports, pendingReports, resolvedReports) =
            await _adminRepo.GetReportMetricCountsAsync();

        var recentStudents = await _adminRepo.GetRecentStudentsAsync(5);
        var recentBooks = await _adminRepo.GetRecentBooksAsync(5);
        var recentRequests = await _adminRepo.GetRecentRequestsAsync(5);
        var recentReports = await _adminRepo.GetRecentReportsAsync(5);
        var categoryDistribution = await _adminRepo.GetCategoryDistributionAsync();

        return new AdminDashboardViewModel
        {
            TotalStudents = totalStudents,
            ActiveStudents = activeStudents,
            SuspendedStudents = suspendedStudents,
            VerifiedStudents = verifiedStudents,
            PendingVerificationStudents = pendingStudents,
            RejectedVerificationStudents = rejectedStudents,

            TotalBooks = totalBooks,
            AvailableBooks = availableBooks,
            ReservedBooks = reservedBooks,
            CompletedBooks = completedBooks,
            InactiveBooks = inactiveBooks,

            TotalRequests = totalRequests,
            PendingRequests = pendingRequests,
            AcceptedRequests = acceptedRequests,
            RejectedRequests = rejectedRequests,
            CancelledRequests = cancelledRequests,
            CompletedRequests = completedRequests,
            CompletedSales = completedSales,
            CompletedExchanges = completedExchanges,

            TotalReports = totalReports,
            PendingReports = pendingReports,
            ResolvedReports = resolvedReports,

            RecentStudents = recentStudents,
            RecentBooks = recentBooks,
            RecentRequests = recentRequests,
            RecentReports = recentReports,
            CategoryDistribution = categoryDistribution
        };
    }

    // --- Students ---
    public async Task<StudentListViewModel> GetStudentsPagedAsync(
        string? search,
        bool? isActive,
        StudentVerificationStatus? verificationStatus,
        string? sortBy,
        int pageIndex,
        int pageSize)
    {
        var (items, totalCount) = await _adminRepo.GetStudentsPagedAsync(search, isActive, verificationStatus, sortBy, pageIndex, pageSize);

        return new StudentListViewModel
        {
            Students = items,
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize,
            SearchTerm = search,
            IsActive = isActive,
            VerificationStatus = verificationStatus,
            SortBy = sortBy
        };
    }

    public async Task<StudentDetailsViewModel?> GetStudentDetailsAsync(int userId)
    {
        var user = await _adminRepo.GetStudentDetailsAsync(userId);
        if (user == null) return null;

        var permissions = await _permissionService.GetUserPermissionsAsync(userId);
        var completedExchanges = await _dbContext.BookRequests
            .CountAsync(r => (r.RequesterId == userId || r.Book.OwnerId == userId) && r.Status == RequestStatus.Completed);

        return new StudentDetailsViewModel
        {
            User = user,
            TotalListings = user.Books.Count,
            TotalSentRequests = user.SentRequests.Count,
            CompletedExchanges = completedExchanges,
            EffectivePermissions = permissions
        };
    }

    public async Task<ServiceResult> VerifyStudentAsync(int userId, StudentVerificationStatus status, string? notes, int adminId, string? ipAddress)
    {
        var success = await _adminRepo.VerifyStudentAsync(userId, status, notes, adminId);
        if (!success)
        {
            return ServiceResult.Fail("Student profile could not be found.");
        }

        await _auditLogService.LogAsync(
            adminId,
            status == StudentVerificationStatus.Verified ? "Student.Verified" : "Student.VerificationRejected",
            "StudentProfile",
            userId.ToString(),
            $"Status: {status}; Notes: {notes ?? "N/A"}",
            ipAddress);

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> SuspendStudentAsync(int userId, string reason, int adminId, string? ipAddress)
    {
        if (userId == adminId)
        {
            return ServiceResult.Fail("Administrators cannot suspend their own account.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return ServiceResult.Fail("A valid suspension reason is required.");
        }

        var success = await _adminRepo.SuspendStudentAsync(userId, reason, adminId);
        if (!success)
        {
            return ServiceResult.Fail("User account could not be found.");
        }

        // Invalidate permissions cache immediately to evict cookie authorization
        _permissionService.InvalidateUserPermissionsCache(userId);

        await _auditLogService.LogAsync(
            adminId,
            "Student.Suspended",
            "User",
            userId.ToString(),
            $"Reason: {reason}",
            ipAddress);

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> ReactivateStudentAsync(int userId, int adminId, string? ipAddress)
    {
        var success = await _adminRepo.ReactivateStudentAsync(userId, adminId);
        if (!success)
        {
            return ServiceResult.Fail("User account could not be found.");
        }

        _permissionService.InvalidateUserPermissionsCache(userId);

        await _auditLogService.LogAsync(
            adminId,
            "Student.Reactivated",
            "User",
            userId.ToString(),
            "Account reactivated by administrator.",
            ipAddress);

        return ServiceResult.Ok();
    }

    // --- Book Moderation ---
    public async Task<BookModerationListViewModel> GetBooksPagedAsync(string? search, int? categoryId, BookStatus? status, int pageIndex, int pageSize)
    {
        var (items, totalCount) = await _adminRepo.GetBooksPagedAsync(search, categoryId, status, pageIndex, pageSize);
        var categories = await _dbContext.BookCategories.OrderBy(c => c.Name).ToListAsync();

        return new BookModerationListViewModel
        {
            Books = items,
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize,
            SearchTerm = search,
            CategoryId = categoryId,
            Status = status,
            Categories = categories
        };
    }

    public async Task<Book?> GetBookDetailsAsync(int bookId)
    {
        return await _adminRepo.GetBookDetailsAsync(bookId);
    }

    public async Task<ServiceResult> ModerateBookAsync(int bookId, BookStatus status, string? reason, int adminId, string? ipAddress)
    {
        var book = await _adminRepo.GetBookDetailsAsync(bookId);
        if (book == null)
        {
            return ServiceResult.Fail("Book listing could not be found.");
        }

        if (book.Status == BookStatus.Reserved && status == BookStatus.Inactive)
        {
            _logger.LogWarning("Admin {AdminId} archived reserved book {BookId}", adminId, bookId);
        }

        var success = await _adminRepo.ModerateBookAsync(bookId, status, reason, adminId);
        if (!success)
        {
            return ServiceResult.Fail("Could not update listing status.");
        }

        var actionName = status switch
        {
            BookStatus.Available => "Book.Approved",
            BookStatus.Inactive => "Book.Archived",
            _ => "Book.Moderated"
        };

        await _auditLogService.LogAsync(
            adminId,
            actionName,
            "Book",
            bookId.ToString(),
            $"Status set to {status}. Reason: {reason ?? "N/A"}",
            ipAddress);

        return ServiceResult.Ok();
    }

    // --- Classifications ---
    public async Task<CategoryManagementViewModel> GetCategoriesAsync()
    {
        var categories = await _adminRepo.GetCategoriesWithCountsAsync();
        return new CategoryManagementViewModel { Categories = categories };
    }

    public async Task<CategoryEditViewModel?> GetCategoryForEditAsync(int id)
    {
        var cat = await _dbContext.BookCategories.FindAsync(id);
        if (cat == null) return null;

        return new CategoryEditViewModel
        {
            CategoryId = cat.CategoryId,
            Name = cat.Name,
            Description = cat.Description,
            IsActive = cat.IsActive
        };
    }

    public async Task<ServiceResult> SaveCategoryAsync(CategoryEditViewModel model, int adminId, string? ipAddress)
    {
        if (await _adminRepo.CategoryExistsAsync(model.Name, model.CategoryId > 0 ? model.CategoryId : null))
        {
            return ServiceResult.Fail($"A category with the name '{model.Name}' already exists.");
        }

        if (model.CategoryId > 0)
        {
            var category = await _dbContext.BookCategories.FindAsync(model.CategoryId);
            if (category == null) return ServiceResult.Fail("Category not found.");

            category.Name = model.Name.Trim();
            category.Description = model.Description?.Trim();
            category.IsActive = model.IsActive;

            await _dbContext.SaveChangesAsync();

            await _auditLogService.LogAsync(adminId, "Category.Updated", "BookCategory", category.CategoryId.ToString(), $"Updated name: {category.Name}", ipAddress);
        }
        else
        {
            var category = new BookCategory
            {
                Name = model.Name.Trim(),
                Description = model.Description?.Trim(),
                IsActive = model.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            await _dbContext.BookCategories.AddAsync(category);
            await _dbContext.SaveChangesAsync();

            await _auditLogService.LogAsync(adminId, "Category.Created", "BookCategory", category.CategoryId.ToString(), $"Created category: {category.Name}", ipAddress);
        }

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> ToggleCategoryStatusAsync(int id, int adminId, string? ipAddress)
    {
        var category = await _dbContext.BookCategories.FindAsync(id);
        if (category == null) return ServiceResult.Fail("Category not found.");

        category.IsActive = !category.IsActive;
        await _dbContext.SaveChangesAsync();

        await _auditLogService.LogAsync(adminId, "Category.StatusToggled", "BookCategory", id.ToString(), $"Active: {category.IsActive}", ipAddress);
        return ServiceResult.Ok();
    }

    public async Task<DepartmentManagementViewModel> GetDepartmentsAsync()
    {
        var depts = await _adminRepo.GetDepartmentsWithCountsAsync();
        return new DepartmentManagementViewModel { Departments = depts };
    }

    public async Task<DepartmentEditViewModel?> GetDepartmentForEditAsync(int id)
    {
        var dept = await _dbContext.Departments.FindAsync(id);
        if (dept == null) return null;

        return new DepartmentEditViewModel
        {
            DepartmentId = dept.DepartmentId,
            Name = dept.Name,
            Code = dept.Code,
            IsActive = dept.IsActive
        };
    }

    public async Task<ServiceResult> SaveDepartmentAsync(DepartmentEditViewModel model, int adminId, string? ipAddress)
    {
        if (model.DepartmentId > 0)
        {
            var dept = await _dbContext.Departments.FindAsync(model.DepartmentId);
            if (dept == null) return ServiceResult.Fail("Department not found.");

            dept.Name = model.Name.Trim();
            dept.Code = model.Code.Trim().ToUpperInvariant();
            dept.IsActive = model.IsActive;

            await _dbContext.SaveChangesAsync();
            await _auditLogService.LogAsync(adminId, "Department.Updated", "Department", dept.DepartmentId.ToString(), $"Updated: {dept.Code}", ipAddress);
        }
        else
        {
            var dept = new Department
            {
                Name = model.Name.Trim(),
                Code = model.Code.Trim().ToUpperInvariant(),
                IsActive = model.IsActive
            };

            await _dbContext.Departments.AddAsync(dept);
            await _dbContext.SaveChangesAsync();
            await _auditLogService.LogAsync(adminId, "Department.Created", "Department", dept.DepartmentId.ToString(), $"Created: {dept.Code}", ipAddress);
        }

        return ServiceResult.Ok();
    }

    public async Task<CourseManagementViewModel> GetCoursesAsync(int? departmentId)
    {
        var courses = await _adminRepo.GetCoursesAsync(departmentId);
        var departments = await _dbContext.Departments.OrderBy(d => d.Name).ToListAsync();

        return new CourseManagementViewModel
        {
            Courses = courses,
            Departments = departments,
            SelectedDepartmentId = departmentId
        };
    }

    public async Task<CourseEditViewModel?> GetCourseForEditAsync(int id)
    {
        var course = await _dbContext.Courses.FindAsync(id);
        if (course == null) return null;

        return new CourseEditViewModel
        {
            CourseId = course.CourseId,
            DepartmentId = course.DepartmentId,
            CourseCode = course.CourseCode,
            CourseName = course.CourseName,
            IsActive = course.IsActive
        };
    }

    public async Task<ServiceResult> SaveCourseAsync(CourseEditViewModel model, int adminId, string? ipAddress)
    {
        if (await _adminRepo.CourseExistsAsync(model.DepartmentId, model.CourseCode, model.CourseId > 0 ? model.CourseId : null))
        {
            return ServiceResult.Fail($"Course code '{model.CourseCode}' already exists for this department.");
        }

        if (model.CourseId > 0)
        {
            var course = await _dbContext.Courses.FindAsync(model.CourseId);
            if (course == null) return ServiceResult.Fail("Course not found.");

            course.DepartmentId = model.DepartmentId;
            course.CourseCode = model.CourseCode.Trim().ToUpperInvariant();
            course.CourseName = model.CourseName.Trim();
            course.IsActive = model.IsActive;

            await _dbContext.SaveChangesAsync();
            await _auditLogService.LogAsync(adminId, "Course.Updated", "Course", course.CourseId.ToString(), $"Updated: {course.CourseCode}", ipAddress);
        }
        else
        {
            var course = new Course
            {
                DepartmentId = model.DepartmentId,
                CourseCode = model.CourseCode.Trim().ToUpperInvariant(),
                CourseName = model.CourseName.Trim(),
                IsActive = model.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            await _dbContext.Courses.AddAsync(course);
            await _dbContext.SaveChangesAsync();
            await _auditLogService.LogAsync(adminId, "Course.Created", "Course", course.CourseId.ToString(), $"Created: {course.CourseCode}", ipAddress);
        }

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> ToggleCourseStatusAsync(int id, int adminId, string? ipAddress)
    {
        var course = await _dbContext.Courses.FindAsync(id);
        if (course == null) return ServiceResult.Fail("Course not found.");

        course.IsActive = !course.IsActive;
        await _dbContext.SaveChangesAsync();
        await _auditLogService.LogAsync(adminId, "Course.StatusToggled", "Course", id.ToString(), $"Active: {course.IsActive}", ipAddress);
        return ServiceResult.Ok();
    }

    // --- Requests & Exchanges ---
    public async Task<RequestMonitoringListViewModel> GetRequestsPagedAsync(string? search, RequestStatus? status, ExchangeType? type, int pageIndex, int pageSize)
    {
        var (items, totalCount) = await _adminRepo.GetRequestsPagedAsync(search, status, type, pageIndex, pageSize);

        return new RequestMonitoringListViewModel
        {
            Requests = items,
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize,
            SearchTerm = search,
            Status = status,
            Type = type
        };
    }

    public async Task<BookRequest?> GetRequestDetailsAsync(int requestId)
    {
        return await _adminRepo.GetRequestDetailsAsync(requestId);
    }

    public async Task<ServiceResult> CancelRequestAsAdminAsync(int requestId, string reason, int adminId, string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return ServiceResult.Fail("A cancellation reason is required.");
        }

        var success = await _adminRepo.CancelRequestAsAdminAsync(requestId, reason, adminId);
        if (!success)
        {
            return ServiceResult.Fail("Request could not be cancelled. Completed transactions cannot be cancelled.");
        }

        await _auditLogService.LogAsync(adminId, "Request.AdminCancelled", "BookRequest", requestId.ToString(), $"Reason: {reason}", ipAddress);
        return ServiceResult.Ok();
    }

    // --- Reports ---
    public async Task<ReportListViewModel> GetReportsPagedAsync(ReportStatus? status, ReportTargetType? targetType, int pageIndex, int pageSize)
    {
        var (items, totalCount) = await _adminRepo.GetReportsPagedAsync(status, targetType, pageIndex, pageSize);

        return new ReportListViewModel
        {
            Reports = items,
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize,
            Status = status,
            TargetType = targetType
        };
    }

    public async Task<Report?> GetReportDetailsAsync(int reportId)
    {
        return await _adminRepo.GetReportDetailsAsync(reportId);
    }

    public async Task<ServiceResult> ResolveReportAsync(int reportId, string resolutionNotes, int adminId, string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(resolutionNotes))
        {
            return ServiceResult.Fail("Resolution notes are required.");
        }

        var success = await _adminRepo.UpdateReportStatusAsync(reportId, ReportStatus.Resolved, resolutionNotes, adminId);
        if (!success) return ServiceResult.Fail("Report not found.");

        await _auditLogService.LogAsync(adminId, "Report.Resolved", "Report", reportId.ToString(), $"Notes: {resolutionNotes}", ipAddress);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> DismissReportAsync(int reportId, string resolutionNotes, int adminId, string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(resolutionNotes))
        {
            return ServiceResult.Fail("Resolution notes are required.");
        }

        var success = await _adminRepo.UpdateReportStatusAsync(reportId, ReportStatus.Dismissed, resolutionNotes, adminId);
        if (!success) return ServiceResult.Fail("Report not found.");

        await _auditLogService.LogAsync(adminId, "Report.Dismissed", "Report", reportId.ToString(), $"Notes: {resolutionNotes}", ipAddress);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> SubmitReportAsync(int reporterId, CreateReportViewModel model)
    {
        var report = new Report
        {
            ReporterId = reporterId,
            TargetType = model.TargetType,
            TargetId = model.TargetId,
            Reason = model.Reason.Trim(),
            Description = model.Description.Trim(),
            Status = ReportStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await _adminRepo.AddReportAsync(report);
        return ServiceResult.Ok();
    }

    // --- Reviews ---
    public async Task<ReviewModerationListViewModel> GetReviewsPagedAsync(bool? isHidden, int pageIndex, int pageSize)
    {
        var (items, totalCount) = await _adminRepo.GetReviewsPagedAsync(isHidden, pageIndex, pageSize);

        return new ReviewModerationListViewModel
        {
            Reviews = items,
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize,
            IsHidden = isHidden
        };
    }

    public async Task<ServiceResult> SetReviewVisibilityAsync(int reviewId, bool isHidden, string? reason, int adminId, string? ipAddress)
    {
        var success = await _adminRepo.SetReviewVisibilityAsync(reviewId, isHidden, reason, adminId);
        if (!success) return ServiceResult.Fail("Review not found.");

        var action = isHidden ? "Review.Hidden" : "Review.Restored";
        await _auditLogService.LogAsync(adminId, action, "ExchangeReview", reviewId.ToString(), $"Hidden: {isHidden}; Reason: {reason ?? "N/A"}", ipAddress);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> SubmitReviewAsync(int reviewerId, CreateReviewViewModel model)
    {
        var request = await _dbContext.BookRequests
            .Include(r => r.Book)
            .FirstOrDefaultAsync(r => r.RequestId == model.RequestId);

        if (request == null || request.Status != RequestStatus.Completed)
        {
            return ServiceResult.Fail("Reviews can only be submitted for completed exchanges.");
        }

        // Verify participant
        var isParticipant = request.RequesterId == reviewerId || request.Book.OwnerId == reviewerId;
        if (!isParticipant)
        {
            return ServiceResult.Fail("You can only review exchanges in which you participated.");
        }

        // Check if already reviewed
        var alreadyReviewed = await _dbContext.ExchangeReviews
            .AnyAsync(r => r.RequestId == model.RequestId && r.ReviewerId == reviewerId);

        if (alreadyReviewed)
        {
            return ServiceResult.Fail("You have already reviewed this exchange.");
        }

        var reviewedUserId = request.RequesterId == reviewerId ? request.Book.OwnerId : request.RequesterId;

        var review = new ExchangeReview
        {
            RequestId = model.RequestId,
            ReviewerId = reviewerId,
            ReviewedUserId = reviewedUserId,
            Rating = Math.Clamp(model.Rating, 1, 5),
            Comment = model.Comment.Trim(),
            IsHidden = false,
            CreatedAt = DateTime.UtcNow
        };

        await _adminRepo.AddReviewAsync(review);
        return ServiceResult.Ok();
    }

    // --- Analytics ---
    public async Task<AnalyticsDashboardViewModel> GetAnalyticsAsync()
    {
        var (totalStudents, _, suspendedStudents, verifiedStudents, _, _) = await _adminRepo.GetStudentMetricCountsAsync();
        var (totalBooks, _, _, _, _) = await _adminRepo.GetBookMetricCountsAsync();
        var (totalRequests, _, acceptedRequests, rejectedRequests, cancelledRequests, _) = await _adminRepo.GetRequestMetricCountsAsync();
        var (completedSales, completedExchanges) = await _adminRepo.GetCompletedTransactionCountsAsync();
        var (totalReports, _, resolvedReports) = await _adminRepo.GetReportMetricCountsAsync();

        var regTrends = await _adminRepo.GetRegistrationTrendsAsync(6);
        var transTrends = await _adminRepo.GetTransactionTrendsAsync(6);
        var catDist = await _adminRepo.GetCategoryDistributionAsync();

        return new AnalyticsDashboardViewModel
        {
            TotalStudents = totalStudents,
            VerifiedStudents = verifiedStudents,
            SuspendedStudents = suspendedStudents,
            TotalBooks = totalBooks,
            CompletedSales = completedSales,
            CompletedExchanges = completedExchanges,
            TotalRequests = totalRequests,
            AcceptedRequests = acceptedRequests,
            RejectedRequests = rejectedRequests,
            CancelledRequests = cancelledRequests,
            TotalReports = totalReports,
            ResolvedReports = resolvedReports,
            RegistrationTrends = regTrends,
            TransactionTrends = transTrends,
            CategoryDistribution = catDist
        };
    }

    public async Task<byte[]> ExportStudentsCsvAsync()
    {
        var students = await _dbContext.Users
            .Include(u => u.StudentProfile)
            .Where(u => u.RoleId == 1)
            .OrderByDescending(u => u.CreatedAt)
            .AsNoTracking()
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("UserId,FullName,StudentId,PersonalEmail,UniversityEmail,Department,Batch,VerificationStatus,IsActive,CreatedAt");

        foreach (var s in students)
        {
            var p = s.StudentProfile;
            sb.AppendLine($"\"{s.UserId}\",\"{EscapeCsv(s.FullName)}\",\"{EscapeCsv(s.StudentId)}\",\"{EscapeCsv(s.Email)}\",\"{EscapeCsv(p?.UniversityEmail)}\",\"{EscapeCsv(p?.Department)}\",\"{EscapeCsv(p?.Batch)}\",\"{p?.VerificationStatus}\",\"{s.IsActive}\",\"{s.CreatedAt:yyyy-MM-dd HH:mm:ss}\"");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<byte[]> ExportTransactionsCsvAsync()
    {
        var requests = await _dbContext.BookRequests
            .Include(r => r.Book)
            .Include(r => r.Requester)
            .Include(r => r.Book.Owner)
            .Where(r => r.Status == RequestStatus.Completed)
            .OrderByDescending(r => r.CreatedAt)
            .AsNoTracking()
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("RequestId,BookTitle,ListingType,Price,OwnerName,RequesterName,Status,CreatedAt,UpdatedAt");

        foreach (var r in requests)
        {
            sb.AppendLine($"\"{r.RequestId}\",\"{EscapeCsv(r.Book.Title)}\",\"{r.Book.ExchangeType}\",\"{r.Book.Price}\",\"{EscapeCsv(r.Book.Owner.FullName)}\",\"{EscapeCsv(r.Requester.FullName)}\",\"{r.Status}\",\"{r.CreatedAt:yyyy-MM-dd HH:mm:ss}\",\"{r.UpdatedAt:yyyy-MM-dd HH:mm:ss}\"");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string EscapeCsv(string? val)
    {
        if (string.IsNullOrEmpty(val)) return string.Empty;
        return val.Replace("\"", "\"\"");
    }
}
