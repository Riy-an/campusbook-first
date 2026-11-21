using CampusBook.DataAccess.Interface;
using CampusBook.Entities;
using Microsoft.EntityFrameworkCore;

namespace CampusBook.DataAccess.Implementation;

public class AdminRepository : IAdminRepository
{
    private readonly ApplicationDbContext _dbContext;

    public AdminRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // --- Dashboard Metrics ---
    public async Task<(int TotalStudents, int ActiveStudents, int SuspendedStudents, int VerifiedStudents, int PendingVerificationStudents, int RejectedVerificationStudents)> GetStudentMetricCountsAsync()
    {
        var total = await _dbContext.Users.CountAsync(u => u.RoleId == 1);
        var active = await _dbContext.Users.CountAsync(u => u.RoleId == 1 && u.IsActive);
        var suspended = await _dbContext.Users.CountAsync(u => u.RoleId == 1 && !u.IsActive);

        var verified = await _dbContext.StudentProfiles.CountAsync(p => p.VerificationStatus == StudentVerificationStatus.Verified);
        var pending = await _dbContext.StudentProfiles.CountAsync(p => p.VerificationStatus == StudentVerificationStatus.Pending);
        var rejected = await _dbContext.StudentProfiles.CountAsync(p => p.VerificationStatus == StudentVerificationStatus.Rejected);

        return (total, active, suspended, verified, pending, rejected);
    }

    public async Task<(int TotalBooks, int AvailableBooks, int ReservedBooks, int CompletedBooks, int InactiveBooks)> GetBookMetricCountsAsync()
    {
        var total = await _dbContext.Books.CountAsync();
        var available = await _dbContext.Books.CountAsync(b => b.Status == BookStatus.Available);
        var reserved = await _dbContext.Books.CountAsync(b => b.Status == BookStatus.Reserved);
        var completed = await _dbContext.Books.CountAsync(b => b.Status == BookStatus.Completed);
        var inactive = await _dbContext.Books.CountAsync(b => b.Status == BookStatus.Inactive);

        return (total, available, reserved, completed, inactive);
    }

    public async Task<(int TotalRequests, int PendingRequests, int AcceptedRequests, int RejectedRequests, int CancelledRequests, int CompletedRequests)> GetRequestMetricCountsAsync()
    {
        var total = await _dbContext.BookRequests.CountAsync();
        var pending = await _dbContext.BookRequests.CountAsync(r => r.Status == RequestStatus.Pending);
        var accepted = await _dbContext.BookRequests.CountAsync(r => r.Status == RequestStatus.Accepted);
        var rejected = await _dbContext.BookRequests.CountAsync(r => r.Status == RequestStatus.Rejected);
        var cancelled = await _dbContext.BookRequests.CountAsync(r => r.Status == RequestStatus.Cancelled);
        var completed = await _dbContext.BookRequests.CountAsync(r => r.Status == RequestStatus.Completed);

        return (total, pending, accepted, rejected, cancelled, completed);
    }

    public async Task<(int CompletedSales, int CompletedExchanges)> GetCompletedTransactionCountsAsync()
    {
        var sales = await _dbContext.BookRequests
            .Include(r => r.Book)
            .CountAsync(r => r.Status == RequestStatus.Completed && r.Book.ExchangeType == ExchangeType.Sale);

        var exchanges = await _dbContext.BookRequests
            .Include(r => r.Book)
            .CountAsync(r => r.Status == RequestStatus.Completed && r.Book.ExchangeType == ExchangeType.Exchange);

        return (sales, exchanges);
    }

    public async Task<(int TotalReports, int PendingReports, int ResolvedReports)> GetReportMetricCountsAsync()
    {
        var total = await _dbContext.Reports.CountAsync();
        var pending = await _dbContext.Reports.CountAsync(r => r.Status == ReportStatus.Pending || r.Status == ReportStatus.UnderReview);
        var resolved = await _dbContext.Reports.CountAsync(r => r.Status == ReportStatus.Resolved);

        return (total, pending, resolved);
    }

    public async Task<IEnumerable<User>> GetRecentStudentsAsync(int count)
    {
        return await _dbContext.Users
            .Include(u => u.StudentProfile)
            .Where(u => u.RoleId == 1)
            .OrderByDescending(u => u.CreatedAt)
            .Take(count)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<Book>> GetRecentBooksAsync(int count)
    {
        return await _dbContext.Books
            .Include(b => b.Category)
            .Include(b => b.Owner)
            .OrderByDescending(b => b.CreatedAt)
            .Take(count)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<BookRequest>> GetRecentRequestsAsync(int count)
    {
        return await _dbContext.BookRequests
            .Include(r => r.Book)
            .Include(r => r.Requester)
            .OrderByDescending(r => r.CreatedAt)
            .Take(count)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<Report>> GetRecentReportsAsync(int count)
    {
        return await _dbContext.Reports
            .Include(r => r.Reporter)
            .OrderByDescending(r => r.CreatedAt)
            .Take(count)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Dictionary<string, int>> GetCategoryDistributionAsync()
    {
        return await _dbContext.BookCategories
            .Select(c => new { c.Name, Count = c.Books.Count })
            .OrderByDescending(x => x.Count)
            .ToDictionaryAsync(x => x.Name, x => x.Count);
    }

    // --- Students Management ---
    public async Task<(IEnumerable<User> Items, int TotalCount)> GetStudentsPagedAsync(
        string? search,
        bool? isActive,
        StudentVerificationStatus? verificationStatus,
        string? sortBy,
        int pageIndex,
        int pageSize)
    {
        var query = _dbContext.Users
            .Include(u => u.StudentProfile)
            .Include(u => u.Role)
            .Where(u => u.RoleId == 1)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(u =>
                u.FullName.ToLower().Contains(term) ||
                u.Email.ToLower().Contains(term) ||
                u.StudentId.ToLower().Contains(term) ||
                (u.StudentProfile != null && u.StudentProfile.UniversityEmail.ToLower().Contains(term)));
        }

        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        if (verificationStatus.HasValue)
        {
            query = query.Where(u => u.StudentProfile != null && u.StudentProfile.VerificationStatus == verificationStatus.Value);
        }

        query = sortBy?.ToLower() switch
        {
            "name_desc" => query.OrderByDescending(u => u.FullName),
            "name_asc" => query.OrderBy(u => u.FullName),
            "date_asc" => query.OrderBy(u => u.CreatedAt),
            _ => query.OrderByDescending(u => u.CreatedAt)
        };

        var total = await query.CountAsync();
        var items = await query.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();

        return (items, total);
    }

    public async Task<User?> GetStudentDetailsAsync(int userId)
    {
        return await _dbContext.Users
            .Include(u => u.StudentProfile)
            .Include(u => u.Books)
            .Include(u => u.SentRequests)
            .Include(u => u.UserPermissions)
                .ThenInclude(up => up.PermissionGroup)
            .Include(u => u.UserPermissions)
                .ThenInclude(up => up.Permission)
            .FirstOrDefaultAsync(u => u.UserId == userId);
    }

    public async Task<bool> VerifyStudentAsync(int userId, StudentVerificationStatus status, string? notes, int adminId)
    {
        var profile = await _dbContext.StudentProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile == null) return false;

        profile.VerificationStatus = status;
        profile.VerificationNotes = notes;
        profile.VerifiedAt = DateTime.UtcNow;
        profile.VerifiedByAdminId = adminId;
        profile.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SuspendStudentAsync(int userId, string reason, int adminId)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null) return false;

        user.IsActive = false;
        user.SuspensionReason = reason;
        user.SuspendedAt = DateTime.UtcNow;
        user.SuspendedByAdminId = adminId;

        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ReactivateStudentAsync(int userId, int adminId)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null) return false;

        user.IsActive = true;
        user.SuspensionReason = null;
        user.SuspendedAt = null;
        user.SuspendedByAdminId = null;

        await _dbContext.SaveChangesAsync();
        return true;
    }

    // --- Book Moderation ---
    public async Task<(IEnumerable<Book> Items, int TotalCount)> GetBooksPagedAsync(
        string? search,
        int? categoryId,
        BookStatus? status,
        int pageIndex,
        int pageSize)
    {
        var query = _dbContext.Books
            .Include(b => b.Category)
            .Include(b => b.Department)
            .Include(b => b.Owner)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(b =>
                b.Title.ToLower().Contains(term) ||
                b.Author.ToLower().Contains(term) ||
                (b.Isbn != null && b.Isbn.ToLower().Contains(term)) ||
                b.Owner.FullName.ToLower().Contains(term));
        }

        if (categoryId.HasValue && categoryId.Value > 0)
        {
            query = query.Where(b => b.CategoryId == categoryId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(b => b.Status == status.Value);
        }

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(b => b.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<Book?> GetBookDetailsAsync(int bookId)
    {
        return await _dbContext.Books
            .Include(b => b.Category)
            .Include(b => b.Department)
            .Include(b => b.Owner)
                .ThenInclude(o => o.StudentProfile)
            .Include(b => b.Requests)
            .FirstOrDefaultAsync(b => b.BookId == bookId);
    }

    public async Task<bool> ModerateBookAsync(int bookId, BookStatus status, string? reason, int adminId)
    {
        var book = await _dbContext.Books.FirstOrDefaultAsync(b => b.BookId == bookId);
        if (book == null) return false;

        book.Status = status;
        book.ModerationReason = reason;
        book.ModeratedAt = DateTime.UtcNow;
        book.ModeratedByAdminId = adminId;
        book.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        return true;
    }

    // --- Classifications ---
    public async Task<IEnumerable<(BookCategory Category, int BookCount)>> GetCategoriesWithCountsAsync()
    {
        var categories = await _dbContext.BookCategories
            .Include(c => c.Books)
            .OrderBy(c => c.Name)
            .AsNoTracking()
            .ToListAsync();

        return categories.Select(c => (c, c.Books.Count));
    }

    public async Task<IEnumerable<(Department Department, int CourseCount, int BookCount)>> GetDepartmentsWithCountsAsync()
    {
        var departments = await _dbContext.Departments
            .Include(d => d.Courses)
            .Include(d => d.Books)
            .OrderBy(d => d.Name)
            .AsNoTracking()
            .ToListAsync();

        return departments.Select(d => (d, d.Courses.Count, d.Books.Count));
    }

    public async Task<IEnumerable<Course>> GetCoursesAsync(int? departmentId)
    {
        var query = _dbContext.Courses
            .Include(c => c.Department)
            .AsNoTracking()
            .AsQueryable();

        if (departmentId.HasValue && departmentId.Value > 0)
        {
            query = query.Where(c => c.DepartmentId == departmentId.Value);
        }

        return await query.OrderBy(c => c.CourseCode).ToListAsync();
    }

    public async Task<bool> CategoryExistsAsync(string name, int? excludeId = null)
    {
        var query = _dbContext.BookCategories.Where(c => c.Name.ToLower() == name.Trim().ToLower());
        if (excludeId.HasValue)
        {
            query = query.Where(c => c.CategoryId != excludeId.Value);
        }
        return await query.AnyAsync();
    }

    public async Task<bool> CourseExistsAsync(int departmentId, string courseCode, int? excludeId = null)
    {
        var query = _dbContext.Courses.Where(c => c.DepartmentId == departmentId && c.CourseCode.ToLower() == courseCode.Trim().ToLower());
        if (excludeId.HasValue)
        {
            query = query.Where(c => c.CourseId != excludeId.Value);
        }
        return await query.AnyAsync();
    }

    // --- Requests & Exchanges ---
    public async Task<(IEnumerable<BookRequest> Items, int TotalCount)> GetRequestsPagedAsync(
        string? search,
        RequestStatus? status,
        ExchangeType? type,
        int pageIndex,
        int pageSize)
    {
        var query = _dbContext.BookRequests
            .Include(r => r.Book)
                .ThenInclude(b => b.Owner)
            .Include(r => r.Requester)
            .Include(r => r.ExchangeOffer)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(r =>
                r.Book.Title.ToLower().Contains(term) ||
                r.Requester.FullName.ToLower().Contains(term) ||
                r.Book.Owner.FullName.ToLower().Contains(term));
        }

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        if (type.HasValue)
        {
            query = query.Where(r => r.Book.ExchangeType == type.Value);
        }

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(r => r.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<BookRequest?> GetRequestDetailsAsync(int requestId)
    {
        return await _dbContext.BookRequests
            .Include(r => r.Book)
                .ThenInclude(b => b.Owner)
                    .ThenInclude(o => o.StudentProfile)
            .Include(r => r.Requester)
                .ThenInclude(req => req.StudentProfile)
            .Include(r => r.ExchangeOffer)
            .Include(r => r.Conversation)
                .ThenInclude(c => c!.Messages)
            .Include(r => r.Reviews)
            .FirstOrDefaultAsync(r => r.RequestId == requestId);
    }

    public async Task<bool> CancelRequestAsAdminAsync(int requestId, string reason, int adminId)
    {
        var request = await _dbContext.BookRequests
            .Include(r => r.Book)
            .Include(r => r.ExchangeOffer)
                .ThenInclude(o => o!.OfferedBookListing)
            .FirstOrDefaultAsync(r => r.RequestId == requestId);

        if (request == null || request.Status == RequestStatus.Completed)
        {
            return false;
        }

        request.Status = RequestStatus.Cancelled;
        request.UpdatedAt = DateTime.UtcNow;

        // Release reservations if it was accepted
        if (request.Book != null && request.Book.Status == BookStatus.Reserved)
        {
            request.Book.Status = BookStatus.Available;
            request.Book.UpdatedAt = DateTime.UtcNow;
        }

        if (request.ExchangeOffer?.OfferedBookListing != null && request.ExchangeOffer.OfferedBookListing.Status == BookStatus.Reserved)
        {
            request.ExchangeOffer.OfferedBookListing.Status = BookStatus.Available;
            request.ExchangeOffer.OfferedBookListing.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync();
        return true;
    }

    // --- Reports ---
    public async Task<(IEnumerable<Report> Items, int TotalCount)> GetReportsPagedAsync(
        ReportStatus? status,
        ReportTargetType? targetType,
        int pageIndex,
        int pageSize)
    {
        var query = _dbContext.Reports
            .Include(r => r.Reporter)
            .Include(r => r.AssignedAdmin)
            .AsNoTracking()
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        if (targetType.HasValue)
        {
            query = query.Where(r => r.TargetType == targetType.Value);
        }

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(r => r.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<Report?> GetReportDetailsAsync(int reportId)
    {
        return await _dbContext.Reports
            .Include(r => r.Reporter)
            .Include(r => r.AssignedAdmin)
            .FirstOrDefaultAsync(r => r.ReportId == reportId);
    }

    public async Task<bool> UpdateReportStatusAsync(int reportId, ReportStatus status, string resolutionNotes, int adminId)
    {
        var report = await _dbContext.Reports.FirstOrDefaultAsync(r => r.ReportId == reportId);
        if (report == null) return false;

        report.Status = status;
        report.ResolutionNotes = resolutionNotes;
        report.AssignedAdminId = adminId;
        if (status == ReportStatus.Resolved || status == ReportStatus.Dismissed)
        {
            report.ResolvedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task AddReportAsync(Report report)
    {
        ArgumentNullException.ThrowIfNull(report);
        report.CreatedAt = DateTime.UtcNow;
        await _dbContext.Reports.AddAsync(report);
        await _dbContext.SaveChangesAsync();
    }

    // --- Reviews ---
    public async Task<(IEnumerable<ExchangeReview> Items, int TotalCount)> GetReviewsPagedAsync(
        bool? isHidden,
        int pageIndex,
        int pageSize)
    {
        var query = _dbContext.ExchangeReviews
            .Include(r => r.Reviewer)
            .Include(r => r.ReviewedUser)
            .Include(r => r.Request)
                .ThenInclude(req => req.Book)
            .AsNoTracking()
            .AsQueryable();

        if (isHidden.HasValue)
        {
            query = query.Where(r => r.IsHidden == isHidden.Value);
        }

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(r => r.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<bool> SetReviewVisibilityAsync(int reviewId, bool isHidden, string? reason, int adminId)
    {
        var review = await _dbContext.ExchangeReviews.FirstOrDefaultAsync(r => r.ReviewId == reviewId);
        if (review == null) return false;

        review.IsHidden = isHidden;
        review.ModerationReason = reason;

        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task AddReviewAsync(ExchangeReview review)
    {
        ArgumentNullException.ThrowIfNull(review);
        review.CreatedAt = DateTime.UtcNow;
        await _dbContext.ExchangeReviews.AddAsync(review);
        await _dbContext.SaveChangesAsync();
    }

    // --- Analytics ---
    public async Task<Dictionary<string, int>> GetRegistrationTrendsAsync(int months)
    {
        var startDate = DateTime.UtcNow.AddMonths(-months);
        var users = await _dbContext.Users
            .Where(u => u.RoleId == 1 && u.CreatedAt >= startDate)
            .Select(u => u.CreatedAt)
            .ToListAsync();

        return users
            .GroupBy(d => d.ToString("yyyy-MM"))
            .OrderBy(g => g.Key)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    public async Task<Dictionary<string, int>> GetTransactionTrendsAsync(int months)
    {
        var startDate = DateTime.UtcNow.AddMonths(-months);
        var requests = await _dbContext.BookRequests
            .Where(r => r.Status == RequestStatus.Completed && r.CreatedAt >= startDate)
            .Select(r => r.CreatedAt)
            .ToListAsync();

        return requests
            .GroupBy(d => d.ToString("yyyy-MM"))
            .OrderBy(g => g.Key)
            .ToDictionary(g => g.Key, g => g.Count());
    }
}
