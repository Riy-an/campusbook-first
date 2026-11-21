using System.Reflection;
using CampusBook.DataAccess;
using CampusBook.DataAccess.Implementation;
using CampusBook.Entities;
using CampusBook.Facade.Implementation;
using CampusBook.Facade.Interface;
using CampusBook.Facade.Services;
using CampusBook.Facade.ViewModels.Admin;
using CampusBook.Framework;
using CampusBook.Web.UI.Authorization;
using CampusBook.Web.UI.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CampusBook.Tests;

public class AdminModuleTests
{
    private static IMemoryCache CreateCache() => new MemoryCache(new MemoryCacheOptions());

    private static (AdminFacade facade, IAuditLogService auditService, ApplicationDbContext context) CreateFacade(string dbName)
    {
        var context = TestDbContextFactory.Create(dbName);
        var adminRepo = new AdminRepository(context);
        var auditRepo = new AuditLogRepository(context);
        var permRepo = new PermissionRepository(context);
        var cache = CreateCache();
        var permService = new PermissionService(permRepo, cache, NullLogger<PermissionService>.Instance);
        var auditService = new AuditLogService(auditRepo, NullLogger<AuditLogService>.Instance);
        var facade = new AdminFacade(adminRepo, auditService, permService, context, NullLogger<AdminFacade>.Instance);
        return (facade, auditService, context);
    }

    [Fact]
    public void AdminController_IsProtectedByAuthorizeAndHasPermissionAttributes()
    {
        var controllerType = typeof(AdminController);

        // Verify class-level [Authorize]
        var classAuthorize = controllerType.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(classAuthorize);

        // Check that action methods have [HasPermission]
        var actionMethods = controllerType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(m => typeof(IActionResult).IsAssignableFrom(m.ReturnType) ||
                        typeof(Task<IActionResult>).IsAssignableFrom(m.ReturnType) ||
                        typeof(FileResult).IsAssignableFrom(m.ReturnType) ||
                        typeof(Task<FileResult>).IsAssignableFrom(m.ReturnType));

        foreach (var method in actionMethods)
        {
            var hasPermission = method.GetCustomAttribute<HasPermissionAttribute>();
            Assert.True(hasPermission != null, $"Method {method.Name} on AdminController should have [HasPermission] attribute.");
        }
    }

    [Fact]
    public async Task AdminDashboard_CalculatesRealTimeAggregatesCorrectly()
    {
        var (facade, _, context) = CreateFacade(nameof(AdminDashboard_CalculatesRealTimeAggregatesCorrectly));

        // Seed students
        var student1 = new User { UserId = 10, FullName = "Student One", Email = "s1@univ.edu", PasswordHash = "hash", RoleId = 1, IsActive = true, CreatedAt = DateTime.UtcNow };
        var student2 = new User { UserId = 11, FullName = "Student Two", Email = "s2@univ.edu", PasswordHash = "hash", RoleId = 1, IsActive = false, CreatedAt = DateTime.UtcNow };
        context.Users.AddRange(student1, student2);

        var profile1 = new StudentProfile { UserId = 10, Department = "CSE", Batch = "2023", UniversityEmail = "s1@univ.edu", VerificationStatus = StudentVerificationStatus.Verified, UpdatedAt = DateTime.UtcNow };
        var profile2 = new StudentProfile { UserId = 11, Department = "EEE", Batch = "2024", UniversityEmail = "s2@univ.edu", VerificationStatus = StudentVerificationStatus.Pending, UpdatedAt = DateTime.UtcNow };
        context.StudentProfiles.AddRange(profile1, profile2);

        // Seed books
        var book1 = new Book { BookId = 10, OwnerId = 10, Title = "C# in Depth", Author = "Jon Skeet", CategoryId = 1, ExchangeType = ExchangeType.Sale, Condition = BookCondition.Good, Status = BookStatus.Available, Price = 250, CreatedAt = DateTime.UtcNow };
        var book2 = new Book { BookId = 11, OwnerId = 10, Title = "Algorithms", Author = "Sedgewick", CategoryId = 1, ExchangeType = ExchangeType.Sale, Condition = BookCondition.Good, Status = BookStatus.Completed, Price = 500, CreatedAt = DateTime.UtcNow };
        context.Books.AddRange(book1, book2);

        // Seed reports
        var report1 = new Report { ReportId = 10, ReporterId = 10, TargetType = ReportTargetType.Book, TargetId = 10, Reason = "Spam", Description = "Test", Status = ReportStatus.Pending, CreatedAt = DateTime.UtcNow };
        context.Reports.Add(report1);

        await context.SaveChangesAsync();

        var dashboard = await facade.GetDashboardAsync();

        Assert.NotNull(dashboard);
        Assert.Equal(2, dashboard.TotalStudents);
        Assert.Equal(1, dashboard.ActiveStudents);
        Assert.Equal(1, dashboard.VerifiedStudents);
        Assert.Equal(1, dashboard.PendingVerificationStudents);
        Assert.Equal(2, dashboard.TotalBooks);
        Assert.Equal(1, dashboard.AvailableBooks);
        Assert.Equal(1, dashboard.PendingReports);
    }

    [Fact]
    public async Task StudentManagement_SearchAndFilter_WorksAccurately()
    {
        var (facade, _, context) = CreateFacade(nameof(StudentManagement_SearchAndFilter_WorksAccurately));

        var u1 = new User { UserId = 21, FullName = "Rahim Ahmed", Email = "rahim@campus.edu", PasswordHash = "hash", RoleId = 1, IsActive = true, CreatedAt = DateTime.UtcNow };
        var u2 = new User { UserId = 22, FullName = "Karim Khan", Email = "karim@campus.edu", PasswordHash = "hash", RoleId = 1, IsActive = true, CreatedAt = DateTime.UtcNow };
        context.Users.AddRange(u1, u2);

        var p1 = new StudentProfile { UserId = 21, Department = "CSE", Batch = "2023", UniversityEmail = "rahim@campus.edu", VerificationStatus = StudentVerificationStatus.Verified, UpdatedAt = DateTime.UtcNow };
        var p2 = new StudentProfile { UserId = 22, Department = "EEE", Batch = "2024", UniversityEmail = "karim@campus.edu", VerificationStatus = StudentVerificationStatus.Pending, UpdatedAt = DateTime.UtcNow };
        context.StudentProfiles.AddRange(p1, p2);
        await context.SaveChangesAsync();

        // Search by query
        var searchResult = await facade.GetStudentsPagedAsync(search: "Rahim", isActive: null, verificationStatus: null, sortBy: null, pageIndex: 1, pageSize: 10);
        var studentList = searchResult.Students.ToList();
        Assert.Single(studentList);
        Assert.Equal("Rahim Ahmed", studentList[0].FullName);

        // Filter by verification status
        var pendingResult = await facade.GetStudentsPagedAsync(search: null, isActive: null, verificationStatus: StudentVerificationStatus.Pending, sortBy: null, pageIndex: 1, pageSize: 10);
        var pendingList = pendingResult.Students.ToList();
        Assert.Single(pendingList);
        Assert.Equal("Karim Khan", pendingList[0].FullName);
    }

    [Fact]
    public async Task StudentVerification_StatusTransitions_RecordAudits()
    {
        var (facade, _, context) = CreateFacade(nameof(StudentVerification_StatusTransitions_RecordAudits));

        var user = new User { UserId = 30, FullName = "Student Verify", Email = "v@univ.edu", PasswordHash = "h", RoleId = 1, IsActive = true, CreatedAt = DateTime.UtcNow };
        var profile = new StudentProfile { UserId = 30, Department = "CSE", Batch = "2024", UniversityEmail = "v@univ.edu", VerificationStatus = StudentVerificationStatus.Pending, UpdatedAt = DateTime.UtcNow };
        context.Users.Add(user);
        context.StudentProfiles.Add(profile);
        await context.SaveChangesAsync();

        // Verify student
        var verifySuccess = await facade.VerifyStudentAsync(30, StudentVerificationStatus.Verified, "ID card matched registrar records", adminId: 1, ipAddress: "127.0.0.1");
        Assert.True(verifySuccess.Success);

        var updatedProfile = await context.StudentProfiles.FindAsync(30);
        Assert.Equal(StudentVerificationStatus.Verified, updatedProfile!.VerificationStatus);
        Assert.Equal(1, updatedProfile.VerifiedByAdminId);
        Assert.NotNull(updatedProfile.VerifiedAt);

        // Check audit log
        var audits = await context.AuditLogs.Where(a => a.TargetId == "30").ToListAsync();
        Assert.Contains(audits, a => a.Action.Contains("Verif", StringComparison.OrdinalIgnoreCase));

        // Reject student
        var rejectSuccess = await facade.VerifyStudentAsync(30, StudentVerificationStatus.Rejected, "Blurred photo", adminId: 1, ipAddress: "127.0.0.1");
        Assert.True(rejectSuccess.Success);

        var rejectedProfile = await context.StudentProfiles.FindAsync(30);
        Assert.Equal(StudentVerificationStatus.Rejected, rejectedProfile!.VerificationStatus);
        Assert.Equal("Blurred photo", rejectedProfile.VerificationNotes);
    }

    [Fact]
    public async Task StudentSuspension_SuspendsUser_AndReactivates_Correctly()
    {
        var (facade, _, context) = CreateFacade(nameof(StudentSuspension_SuspendsUser_AndReactivates_Correctly));

        var user = new User { UserId = 40, FullName = "Student Suspend", Email = "suspend@univ.edu", PasswordHash = "h", RoleId = 1, IsActive = true, CreatedAt = DateTime.UtcNow };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Suspend
        var suspendResult = await facade.SuspendStudentAsync(40, reason: "Spamming marketplace", adminId: 1, ipAddress: "127.0.0.1");
        Assert.True(suspendResult.Success);

        var suspendedUser = await context.Users.FindAsync(40);
        Assert.False(suspendedUser!.IsActive);
        Assert.Equal(1, suspendedUser.SuspendedByAdminId);
        Assert.Equal("Spamming marketplace", suspendedUser.SuspensionReason);
        Assert.NotNull(suspendedUser.SuspendedAt);

        // Reactivate
        var reactivateResult = await facade.ReactivateStudentAsync(40, adminId: 1, ipAddress: "127.0.0.1");
        Assert.True(reactivateResult.Success);

        var reactivatedUser = await context.Users.FindAsync(40);
        Assert.True(reactivatedUser!.IsActive);
        Assert.Null(reactivatedUser.SuspendedByAdminId);
        Assert.Null(reactivatedUser.SuspensionReason);
        Assert.Null(reactivatedUser.SuspendedAt);
    }

    [Fact]
    public async Task StudentSuspension_PreventsSelfSuspension()
    {
        var (facade, _, context) = CreateFacade(nameof(StudentSuspension_PreventsSelfSuspension));

        var adminUser = new User { UserId = 50, FullName = "Admin User", Email = "admin50@univ.edu", PasswordHash = "h", RoleId = 2, IsActive = true, CreatedAt = DateTime.UtcNow };
        context.Users.Add(adminUser);
        await context.SaveChangesAsync();

        // Attempt to suspend oneself
        var result = await facade.SuspendStudentAsync(50, reason: "Self test", adminId: 50, ipAddress: "127.0.0.1");

        Assert.False(result.Success);
        Assert.Equal("Administrators cannot suspend their own account.", result.ErrorMessage);
    }

    [Fact]
    public async Task BookModeration_ApproveRejectArchive_WorksAsExpected()
    {
        var (facade, _, context) = CreateFacade(nameof(BookModeration_ApproveRejectArchive_WorksAsExpected));

        var owner = new User { UserId = 1, FullName = "Listing Owner", Email = "owner@univ.edu", PasswordHash = "h", RoleId = 1, IsActive = true, CreatedAt = DateTime.UtcNow };
        context.Users.Add(owner);
        var ownerProfile = new StudentProfile { UserId = 1, Department = "CSE", Batch = "2023", UniversityEmail = "owner@univ.edu", UpdatedAt = DateTime.UtcNow };
        context.StudentProfiles.Add(ownerProfile);

        var book = new Book { BookId = 60, OwnerId = 1, Title = "Suspicious Book", Author = "Unknown", CategoryId = 1, ExchangeType = ExchangeType.Sale, Condition = BookCondition.Fair, Status = BookStatus.Available, Price = 0, CreatedAt = DateTime.UtcNow };
        context.Books.Add(book);
        await context.SaveChangesAsync();

        // Reject listing with reason (sets status to Inactive)
        var rejectSuccess = await facade.ModerateBookAsync(60, BookStatus.Inactive, "Prohibited content", adminId: 1, ipAddress: "127.0.0.1");
        Assert.True(rejectSuccess.Success);

        var rejectedBook = await context.Books.FindAsync(60);
        Assert.Equal(BookStatus.Inactive, rejectedBook!.Status);
        Assert.Equal("Prohibited content", rejectedBook.ModerationReason);
        Assert.Equal(1, rejectedBook.ModeratedByAdminId);

        // Approve listing (sets status to Available)
        var approveSuccess = await facade.ModerateBookAsync(60, BookStatus.Available, "Approved by moderator", adminId: 1, ipAddress: "127.0.0.1");
        Assert.True(approveSuccess.Success);

        var approvedBook = await context.Books.FindAsync(60);
        Assert.Equal(BookStatus.Available, approvedBook!.Status);
        Assert.Equal("Approved by moderator", approvedBook.ModerationReason);
    }

    [Fact]
    public async Task Classifications_CategoryAndCourseManagement()
    {
        var (facade, _, context) = CreateFacade(nameof(Classifications_CategoryAndCourseManagement));

        // Create new category
        var catVm = new CategoryEditViewModel { Name = "Cybersecurity", Description = "Security and Cryptography", IsActive = true };
        var catResult = await facade.SaveCategoryAsync(catVm, adminId: 1, ipAddress: "127.0.0.1");
        Assert.True(catResult.Success);

        var createdCat = await context.BookCategories.FirstOrDefaultAsync(c => c.Name == "Cybersecurity");
        Assert.NotNull(createdCat);

        // Try creating duplicate category
        var dupResult = await facade.SaveCategoryAsync(catVm, adminId: 1, ipAddress: "127.0.0.1");
        Assert.False(dupResult.Success);
        Assert.Contains("already exists", dupResult.ErrorMessage);

        // Toggle category
        var toggleCat = await facade.ToggleCategoryStatusAsync(createdCat.CategoryId, adminId: 1, ipAddress: "127.0.0.1");
        Assert.True(toggleCat.Success);
        var toggledCat = await context.BookCategories.FindAsync(createdCat.CategoryId);
        Assert.False(toggledCat!.IsActive);

        // Create Course under department 1
        var courseVm = new CourseEditViewModel { CourseCode = "CSE 401", CourseName = "Advanced Network Security", DepartmentId = 1, IsActive = true };
        var courseResult = await facade.SaveCourseAsync(courseVm, adminId: 1, ipAddress: "127.0.0.1");
        Assert.True(courseResult.Success);

        var createdCourse = await context.Courses.FirstOrDefaultAsync(c => c.CourseCode == "CSE 401");
        Assert.NotNull(createdCourse);
        Assert.Equal(1, createdCourse.DepartmentId);
    }

    [Fact]
    public async Task RequestMonitoring_CancelRequest_ReleasesBook()
    {
        var (facade, _, context) = CreateFacade(nameof(RequestMonitoring_CancelRequest_ReleasesBook));

        var book = new Book { BookId = 70, OwnerId = 1, Title = "Data Structures", Author = "Weiss", CategoryId = 1, ExchangeType = ExchangeType.Sale, Condition = BookCondition.Good, Status = BookStatus.Reserved, Price = 0, CreatedAt = DateTime.UtcNow };
        context.Books.Add(book);

        var request = new BookRequest { RequestId = 70, BookId = 70, RequesterId = 2, Status = RequestStatus.Accepted, CreatedAt = DateTime.UtcNow, RequestMessage = "Library meetup" };
        context.BookRequests.Add(request);
        await context.SaveChangesAsync();

        var cancelSuccess = await facade.CancelRequestAsAdminAsync(70, reason: "Both parties unresponsive for 2 weeks", adminId: 1, ipAddress: "127.0.0.1");
        Assert.True(cancelSuccess.Success);

        var updatedRequest = await context.BookRequests.FindAsync(70);
        Assert.Equal(RequestStatus.Cancelled, updatedRequest!.Status);

        var updatedBook = await context.Books.FindAsync(70);
        Assert.Equal(BookStatus.Available, updatedBook!.Status);
    }

    [Fact]
    public async Task ReportLifecycle_ResolveAndDismiss()
    {
        var (facade, _, context) = CreateFacade(nameof(ReportLifecycle_ResolveAndDismiss));

        var report = new Report { ReportId = 80, ReporterId = 1, TargetType = ReportTargetType.User, TargetId = 5, Reason = "Scam attempt", Description = "User did not show up and asked for money", Status = ReportStatus.Pending, CreatedAt = DateTime.UtcNow };
        context.Reports.Add(report);
        await context.SaveChangesAsync();

        // Resolve report
        var resolveSuccess = await facade.ResolveReportAsync(80, resolutionNotes: "User warning issued", adminId: 1, ipAddress: "127.0.0.1");
        Assert.True(resolveSuccess.Success);
        var resolved = await context.Reports.FindAsync(80);
        Assert.Equal(ReportStatus.Resolved, resolved!.Status);
        Assert.Equal("User warning issued", resolved.ResolutionNotes);
        Assert.NotNull(resolved.ResolvedAt);
        Assert.Equal(1, resolved.AssignedAdminId);
    }

    [Fact]
    public async Task ReviewModeration_HideAndRestore()
    {
        var (facade, _, context) = CreateFacade(nameof(ReviewModeration_HideAndRestore));

        var req = new BookRequest { RequestId = 90, BookId = 1, RequesterId = 2, Status = RequestStatus.Completed, CreatedAt = DateTime.UtcNow };
        context.BookRequests.Add(req);

        var review = new ExchangeReview { ReviewId = 90, RequestId = 90, ReviewerId = 2, ReviewedUserId = 1, Rating = 1, Comment = "Extremely offensive language here", IsHidden = false, CreatedAt = DateTime.UtcNow };
        context.ExchangeReviews.Add(review);
        await context.SaveChangesAsync();

        // Hide review
        var hideSuccess = await facade.SetReviewVisibilityAsync(90, isHidden: true, reason: "Profanity policy violation", adminId: 1, ipAddress: "127.0.0.1");
        Assert.True(hideSuccess.Success);
        var hiddenReview = await context.ExchangeReviews.FindAsync(90);
        Assert.True(hiddenReview!.IsHidden);
        Assert.Equal("Profanity policy violation", hiddenReview.ModerationReason);

        // Restore review
        var restoreSuccess = await facade.SetReviewVisibilityAsync(90, isHidden: false, reason: null, adminId: 1, ipAddress: "127.0.0.1");
        Assert.True(restoreSuccess.Success);
        var restoredReview = await context.ExchangeReviews.FindAsync(90);
        Assert.False(restoredReview!.IsHidden);
    }

    [Fact]
    public async Task AnalyticsAndCsvExport_GeneratesCleanCsvWithoutLeakingSecrets()
    {
        var (facade, _, context) = CreateFacade(nameof(AnalyticsAndCsvExport_GeneratesCleanCsvWithoutLeakingSecrets));

        var u = new User { UserId = 100, FullName = "Export Student", Email = "export@univ.edu", PasswordHash = "VERY_SECRET_HASH", RoleId = 1, IsActive = true, CreatedAt = DateTime.UtcNow };
        context.Users.Add(u);
        var p = new StudentProfile { UserId = 100, Department = "CSE", Batch = "2024", UniversityEmail = "export@univ.edu", VerificationStatus = StudentVerificationStatus.Verified, UpdatedAt = DateTime.UtcNow };
        context.StudentProfiles.Add(p);
        await context.SaveChangesAsync();

        var csvBytes = await facade.ExportStudentsCsvAsync();
        Assert.NotNull(csvBytes);
        Assert.NotEmpty(csvBytes);

        var csvText = System.Text.Encoding.UTF8.GetString(csvBytes);
        Assert.Contains("UserId,FullName,StudentId,PersonalEmail,UniversityEmail,Department,Batch,VerificationStatus,IsActive,CreatedAt", csvText);
        Assert.Contains("Export Student", csvText);
        Assert.Contains("export@univ.edu", csvText);
        // Ensure sensitive secrets are NEVER exported
        Assert.DoesNotContain("VERY_SECRET_HASH", csvText);
    }

    [Fact]
    public async Task AuditLogs_ImmutableLogging_RecordsEveryAdministrativeAction()
    {
        var (facade, auditService, context) = CreateFacade(nameof(AuditLogs_ImmutableLogging_RecordsEveryAdministrativeAction));

        var adminUser = new User { UserId = 200, FullName = "Admin Auditor", Email = "audit@univ.edu", PasswordHash = "h", RoleId = 2, IsActive = true, CreatedAt = DateTime.UtcNow };
        context.Users.Add(adminUser);
        await context.SaveChangesAsync();

        // Perform category addition to trigger audit log
        var catVm = new CategoryEditViewModel { Name = "Data Science", Description = "Machine Learning and Big Data", IsActive = true };
        await facade.SaveCategoryAsync(catVm, adminId: 200, ipAddress: "127.0.0.1");

        var logs = await auditService.GetLogsPagedAsync(action: "Category.Created", adminUserId: null, fromDate: null, toDate: null, pageIndex: 1, pageSize: 10);
        var logList = logs.Logs.ToList();
        Assert.Single(logList);
        Assert.Equal(200, logList[0].AdminUserId);
        Assert.Equal("BookCategory", logList[0].TargetEntityType);
        Assert.True(logList[0].IsSuccess);
    }
}
