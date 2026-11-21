using AppPermissions = CampusBook.Framework.Permissions;
using System.Security.Claims;
using CampusBook.Entities;
using CampusBook.Facade.Interface;
using CampusBook.Facade.Services;
using CampusBook.Facade.ViewModels.Admin;
using CampusBook.Framework;
using CampusBook.Web.UI.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusBook.Web.UI.Controllers;

[Authorize]
public class AdminController : Controller
{
    private readonly IAdminFacade _adminFacade;
    private readonly IPermissionService _permissionService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IAdminFacade adminFacade,
        IPermissionService permissionService,
        IAuditLogService auditLogService,
        ILogger<AdminController> logger)
    {
        _adminFacade = adminFacade;
        _permissionService = permissionService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    private int GetCurrentAdminId()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(idClaim, out var id) ? id : 0;
    }

    private string? GetClientIp()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    // --- Dashboard ---
    [HttpGet]
    [Route(UrlRewrite.Admin.Dashboard)]
    [HasPermission(AppPermissions.Admin.DashboardView)]
    public async Task<IActionResult> Index()
    {
        var model = await _adminFacade.GetDashboardAsync();
        return View(model);
    }

    // --- Students Management ---
    [HttpGet]
    [Route(UrlRewrite.Admin.Students)]
    [HasPermission(AppPermissions.Admin.StudentsView)]
    public async Task<IActionResult> Students(
        string? search,
        bool? isActive,
        StudentVerificationStatus? verificationStatus,
        string? sortBy,
        int page = 1)
    {
        var model = await _adminFacade.GetStudentsPagedAsync(search, isActive, verificationStatus, sortBy, page, 15);
        return View(model);
    }

    [HttpGet]
    [Route(UrlRewrite.Admin.StudentDetails)]
    [HasPermission(AppPermissions.Admin.StudentsView)]
    public async Task<IActionResult> StudentDetails(int id)
    {
        var model = await _adminFacade.GetStudentDetailsAsync(id);
        if (model == null) return NotFound("Student account not found.");

        return View(model);
    }

    [HttpPost]
    [Route(UrlRewrite.Admin.StudentVerify)]
    [HasPermission(AppPermissions.Admin.StudentsVerify)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyStudent(int id, StudentVerificationStatus status, string? notes)
    {
        var adminId = GetCurrentAdminId();
        var result = await _adminFacade.VerifyStudentAsync(id, status, notes, adminId, GetClientIp());

        if (result.Success)
        {
            TempData["SuccessMessage"] = $"Student verification status updated to '{status}'.";
        }
        else
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
        }

        return RedirectToAction(nameof(StudentDetails), new { id });
    }

    [HttpPost]
    [Route(UrlRewrite.Admin.StudentSuspend)]
    [HasPermission(AppPermissions.Admin.StudentsSuspend)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SuspendStudent(int id, string reason)
    {
        var adminId = GetCurrentAdminId();
        var result = await _adminFacade.SuspendStudentAsync(id, reason, adminId, GetClientIp());

        if (result.Success)
        {
            TempData["SuccessMessage"] = "Student account has been suspended.";
        }
        else
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
        }

        return RedirectToAction(nameof(StudentDetails), new { id });
    }

    [HttpPost]
    [Route(UrlRewrite.Admin.StudentReactivate)]
    [HasPermission(AppPermissions.Admin.StudentsSuspend)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReactivateStudent(int id)
    {
        var adminId = GetCurrentAdminId();
        var result = await _adminFacade.ReactivateStudentAsync(id, adminId, GetClientIp());

        if (result.Success)
        {
            TempData["SuccessMessage"] = "Student account has been reactivated.";
        }
        else
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
        }

        return RedirectToAction(nameof(StudentDetails), new { id });
    }

    // --- Book Moderation ---
    [HttpGet]
    [Route(UrlRewrite.Admin.Books)]
    [HasPermission(AppPermissions.Admin.BooksView)]
    public async Task<IActionResult> Books(string? search, int? categoryId, BookStatus? status, int page = 1)
    {
        var model = await _adminFacade.GetBooksPagedAsync(search, categoryId, status, page, 15);
        return View(model);
    }

    [HttpGet]
    [Route(UrlRewrite.Admin.BookDetails)]
    [HasPermission(AppPermissions.Admin.BooksView)]
    public async Task<IActionResult> BookDetails(int id)
    {
        var book = await _adminFacade.GetBookDetailsAsync(id);
        if (book == null) return NotFound("Book listing not found.");

        return View(book);
    }

    [HttpPost]
    [Route(UrlRewrite.Admin.BookApprove)]
    [HasPermission(AppPermissions.Admin.BooksModerate)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveBook(int id)
    {
        var adminId = GetCurrentAdminId();
        var result = await _adminFacade.ModerateBookAsync(id, BookStatus.Available, "Approved by admin.", adminId, GetClientIp());

        if (result.Success)
            TempData["SuccessMessage"] = "Book listing has been approved and marked available.";
        else
            TempData["ErrorMessage"] = result.ErrorMessage;

        return RedirectToAction(nameof(BookDetails), new { id });
    }

    [HttpPost]
    [Route(UrlRewrite.Admin.BookReject)]
    [HasPermission(AppPermissions.Admin.BooksModerate)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectBook(int id, string reason)
    {
        var adminId = GetCurrentAdminId();
        var result = await _adminFacade.ModerateBookAsync(id, BookStatus.Inactive, reason, adminId, GetClientIp());

        if (result.Success)
            TempData["SuccessMessage"] = "Book listing has been rejected.";
        else
            TempData["ErrorMessage"] = result.ErrorMessage;

        return RedirectToAction(nameof(BookDetails), new { id });
    }

    [HttpPost]
    [Route(UrlRewrite.Admin.BookArchive)]
    [HasPermission(AppPermissions.Admin.BooksModerate)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ArchiveBook(int id, string reason)
    {
        var adminId = GetCurrentAdminId();
        var result = await _adminFacade.ModerateBookAsync(id, BookStatus.Inactive, reason, adminId, GetClientIp());

        if (result.Success)
            TempData["SuccessMessage"] = "Book listing has been archived.";
        else
            TempData["ErrorMessage"] = result.ErrorMessage;

        return RedirectToAction(nameof(Books));
    }

    // --- Classifications: Categories ---
    [HttpGet]
    [Route(UrlRewrite.Admin.Categories)]
    [HasPermission(AppPermissions.Admin.CategoriesManage)]
    public async Task<IActionResult> Categories()
    {
        var model = await _adminFacade.GetCategoriesAsync();
        return View(model);
    }

    [HttpGet]
    [Route(UrlRewrite.Admin.CategoryCreate)]
    [HasPermission(AppPermissions.Admin.CategoriesManage)]
    public IActionResult CreateCategory()
    {
        return View("EditCategory", new CategoryEditViewModel { IsActive = true });
    }

    [HttpGet]
    [Route(UrlRewrite.Admin.CategoryEdit)]
    [HasPermission(AppPermissions.Admin.CategoriesManage)]
    public async Task<IActionResult> EditCategory(int id)
    {
        var model = await _adminFacade.GetCategoryForEditAsync(id);
        if (model == null) return NotFound("Category not found.");

        return View(model);
    }

    [HttpPost]
    [Route(UrlRewrite.Admin.CategoryCreate)]
    [Route(UrlRewrite.Admin.CategoryEdit)]
    [HasPermission(AppPermissions.Admin.CategoriesManage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveCategory(CategoryEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("EditCategory", model);
        }

        var adminId = GetCurrentAdminId();
        var result = await _adminFacade.SaveCategoryAsync(model, adminId, GetClientIp());
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Could not save category.");
            return View("EditCategory", model);
        }

        TempData["SuccessMessage"] = "Category saved successfully.";
        return RedirectToAction(nameof(Categories));
    }

    [HttpPost]
    [Route(UrlRewrite.Admin.CategoryToggle)]
    [HasPermission(AppPermissions.Admin.CategoriesManage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleCategory(int id)
    {
        var adminId = GetCurrentAdminId();
        var result = await _adminFacade.ToggleCategoryStatusAsync(id, adminId, GetClientIp());

        if (result.Success)
            TempData["SuccessMessage"] = "Category status toggled.";
        else
            TempData["ErrorMessage"] = result.ErrorMessage;

        return RedirectToAction(nameof(Categories));
    }

    // --- Classifications: Departments & Courses ---
    [HttpGet]
    [Route(UrlRewrite.Admin.Departments)]
    [HasPermission(AppPermissions.Admin.CategoriesManage)]
    public async Task<IActionResult> Departments()
    {
        var model = await _adminFacade.GetDepartmentsAsync();
        return View(model);
    }

    [HttpGet]
    [Route(UrlRewrite.Admin.DepartmentCreate)]
    [HasPermission(AppPermissions.Admin.CategoriesManage)]
    public IActionResult CreateDepartment()
    {
        return View("EditDepartment", new DepartmentEditViewModel { IsActive = true });
    }

    [HttpGet]
    [Route(UrlRewrite.Admin.DepartmentEdit)]
    [HasPermission(AppPermissions.Admin.CategoriesManage)]
    public async Task<IActionResult> EditDepartment(int id)
    {
        var model = await _adminFacade.GetDepartmentForEditAsync(id);
        if (model == null) return NotFound("Department not found.");

        return View(model);
    }

    [HttpPost]
    [Route(UrlRewrite.Admin.DepartmentCreate)]
    [Route(UrlRewrite.Admin.DepartmentEdit)]
    [HasPermission(AppPermissions.Admin.CategoriesManage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveDepartment(DepartmentEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("EditDepartment", model);
        }

        var adminId = GetCurrentAdminId();
        var result = await _adminFacade.SaveDepartmentAsync(model, adminId, GetClientIp());
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Could not save department.");
            return View("EditDepartment", model);
        }

        TempData["SuccessMessage"] = "Department saved successfully.";
        return RedirectToAction(nameof(Departments));
    }

    [HttpGet]
    [Route(UrlRewrite.Admin.Courses)]
    [HasPermission(AppPermissions.Admin.CategoriesManage)]
    public async Task<IActionResult> Courses(int? departmentId)
    {
        var model = await _adminFacade.GetCoursesAsync(departmentId);
        return View(model);
    }

    [HttpGet]
    [Route(UrlRewrite.Admin.CourseCreate)]
    [HasPermission(AppPermissions.Admin.CategoriesManage)]
    public async Task<IActionResult> CreateCourse(int? departmentId)
    {
        var depts = await _adminFacade.GetDepartmentsAsync();
        ViewBag.Departments = depts.Departments.Select(d => d.Department).ToList();

        return View("EditCourse", new CourseEditViewModel
        {
            DepartmentId = departmentId ?? 0,
            IsActive = true
        });
    }

    [HttpGet]
    [Route(UrlRewrite.Admin.CourseEdit)]
    [HasPermission(AppPermissions.Admin.CategoriesManage)]
    public async Task<IActionResult> EditCourse(int id)
    {
        var model = await _adminFacade.GetCourseForEditAsync(id);
        if (model == null) return NotFound("Course not found.");

        var depts = await _adminFacade.GetDepartmentsAsync();
        ViewBag.Departments = depts.Departments.Select(d => d.Department).ToList();

        return View(model);
    }

    [HttpPost]
    [Route(UrlRewrite.Admin.CourseCreate)]
    [Route(UrlRewrite.Admin.CourseEdit)]
    [HasPermission(AppPermissions.Admin.CategoriesManage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveCourse(CourseEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var depts = await _adminFacade.GetDepartmentsAsync();
            ViewBag.Departments = depts.Departments.Select(d => d.Department).ToList();
            return View("EditCourse", model);
        }

        var adminId = GetCurrentAdminId();
        var result = await _adminFacade.SaveCourseAsync(model, adminId, GetClientIp());
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Could not save course.");
            var depts = await _adminFacade.GetDepartmentsAsync();
            ViewBag.Departments = depts.Departments.Select(d => d.Department).ToList();
            return View("EditCourse", model);
        }

        TempData["SuccessMessage"] = "Course classification saved successfully.";
        return RedirectToAction(nameof(Courses), new { departmentId = model.DepartmentId });
    }

    [HttpPost]
    [Route(UrlRewrite.Admin.CourseToggle)]
    [HasPermission(AppPermissions.Admin.CategoriesManage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleCourse(int id)
    {
        var adminId = GetCurrentAdminId();
        var result = await _adminFacade.ToggleCourseStatusAsync(id, adminId, GetClientIp());

        if (result.Success)
            TempData["SuccessMessage"] = "Course status toggled.";
        else
            TempData["ErrorMessage"] = result.ErrorMessage;

        return RedirectToAction(nameof(Courses));
    }

    // --- Requests & Exchanges Monitoring ---
    [HttpGet]
    [Route(UrlRewrite.Admin.Requests)]
    [HasPermission(AppPermissions.Admin.RequestsView)]
    public async Task<IActionResult> Requests(string? search, RequestStatus? status, ExchangeType? type, int page = 1)
    {
        var model = await _adminFacade.GetRequestsPagedAsync(search, status, type, page, 15);
        return View(model);
    }

    [HttpGet]
    [Route(UrlRewrite.Admin.RequestDetails)]
    [HasPermission(AppPermissions.Admin.RequestsView)]
    public async Task<IActionResult> RequestDetails(int id)
    {
        var request = await _adminFacade.GetRequestDetailsAsync(id);
        if (request == null) return NotFound("Request not found.");

        return View(request);
    }

    [HttpPost]
    [Route(UrlRewrite.Admin.RequestIntervene)]
    [HasPermission(AppPermissions.Admin.RequestsView)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelRequest(int id, string reason)
    {
        var adminId = GetCurrentAdminId();
        var result = await _adminFacade.CancelRequestAsAdminAsync(id, reason, adminId, GetClientIp());

        if (result.Success)
            TempData["SuccessMessage"] = "Request was intervened and cancelled; any active reservations were safely released.";
        else
            TempData["ErrorMessage"] = result.ErrorMessage;

        return RedirectToAction(nameof(RequestDetails), new { id });
    }

    [HttpGet]
    [Route(UrlRewrite.Admin.Exchanges)]
    [HasPermission(AppPermissions.Admin.ExchangesView)]
    public async Task<IActionResult> Exchanges(string? search, RequestStatus? status, int page = 1)
    {
        var model = await _adminFacade.GetRequestsPagedAsync(search, status, ExchangeType.Exchange, page, 15);
        return View("Requests", model);
    }

    // --- Reports & Complaints ---
    [HttpGet]
    [Route(UrlRewrite.Admin.Reports)]
    [HasPermission(AppPermissions.Admin.ReportsManage)]
    public async Task<IActionResult> Reports(ReportStatus? status, ReportTargetType? targetType, int page = 1)
    {
        var model = await _adminFacade.GetReportsPagedAsync(status, targetType, page, 15);
        return View(model);
    }

    [HttpGet]
    [Route(UrlRewrite.Admin.ReportDetails)]
    [HasPermission(AppPermissions.Admin.ReportsManage)]
    public async Task<IActionResult> ReportDetails(int id)
    {
        var report = await _adminFacade.GetReportDetailsAsync(id);
        if (report == null) return NotFound("Report not found.");

        return View(report);
    }

    [HttpPost]
    [Route(UrlRewrite.Admin.ReportResolve)]
    [HasPermission(AppPermissions.Admin.ReportsManage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResolveReport(int id, string resolutionNotes)
    {
        var adminId = GetCurrentAdminId();
        var result = await _adminFacade.ResolveReportAsync(id, resolutionNotes, adminId, GetClientIp());

        if (result.Success)
            TempData["SuccessMessage"] = "Report has been marked resolved.";
        else
            TempData["ErrorMessage"] = result.ErrorMessage;

        return RedirectToAction(nameof(ReportDetails), new { id });
    }

    [HttpPost]
    [Route(UrlRewrite.Admin.ReportDismiss)]
    [HasPermission(AppPermissions.Admin.ReportsManage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DismissReport(int id, string resolutionNotes)
    {
        var adminId = GetCurrentAdminId();
        var result = await _adminFacade.DismissReportAsync(id, resolutionNotes, adminId, GetClientIp());

        if (result.Success)
            TempData["SuccessMessage"] = "Report has been dismissed.";
        else
            TempData["ErrorMessage"] = result.ErrorMessage;

        return RedirectToAction(nameof(ReportDetails), new { id });
    }

    // --- Reviews & Ratings ---
    [HttpGet]
    [Route(UrlRewrite.Admin.Reviews)]
    [HasPermission(AppPermissions.Admin.ReviewsModerate)]
    public async Task<IActionResult> Reviews(bool? isHidden, int page = 1)
    {
        var model = await _adminFacade.GetReviewsPagedAsync(isHidden, page, 15);
        return View(model);
    }

    [HttpPost]
    [Route(UrlRewrite.Admin.ReviewHide)]
    [HasPermission(AppPermissions.Admin.ReviewsModerate)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HideReview(int id, string reason)
    {
        var adminId = GetCurrentAdminId();
        var result = await _adminFacade.SetReviewVisibilityAsync(id, true, reason, adminId, GetClientIp());

        if (result.Success)
            TempData["SuccessMessage"] = "Review has been hidden from public display.";
        else
            TempData["ErrorMessage"] = result.ErrorMessage;

        return RedirectToAction(nameof(Reviews));
    }

    [HttpPost]
    [Route(UrlRewrite.Admin.ReviewRestore)]
    [HasPermission(AppPermissions.Admin.ReviewsModerate)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RestoreReview(int id)
    {
        var adminId = GetCurrentAdminId();
        var result = await _adminFacade.SetReviewVisibilityAsync(id, false, null, adminId, GetClientIp());

        if (result.Success)
            TempData["SuccessMessage"] = "Review visibility restored.";
        else
            TempData["ErrorMessage"] = result.ErrorMessage;

        return RedirectToAction(nameof(Reviews));
    }

    // --- Analytics ---
    [HttpGet]
    [Route(UrlRewrite.Admin.Analytics)]
    [HasPermission(AppPermissions.Admin.AnalyticsView)]
    public async Task<IActionResult> Analytics()
    {
        var model = await _adminFacade.GetAnalyticsAsync();
        return View(model);
    }

    [HttpGet]
    [Route(UrlRewrite.Admin.AnalyticsExport + "/students")]
    [HasPermission(AppPermissions.Admin.AnalyticsView)]
    public async Task<IActionResult> ExportStudentsCsv()
    {
        var bytes = await _adminFacade.ExportStudentsCsvAsync();
        return File(bytes, "text/csv", $"CampusBook_Students_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    [HttpGet]
    [Route(UrlRewrite.Admin.AnalyticsExport + "/transactions")]
    [HasPermission(AppPermissions.Admin.AnalyticsView)]
    public async Task<IActionResult> ExportTransactionsCsv()
    {
        var bytes = await _adminFacade.ExportTransactionsCsvAsync();
        return File(bytes, "text/csv", $"CampusBook_Transactions_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    // --- Security & Audit Logs ---
    [HttpGet]
    [Route(UrlRewrite.Admin.AuditLogs)]
    [HasPermission(AppPermissions.Admin.AuditLogsView)]
    public async Task<IActionResult> AuditLogs(string? action, int? adminUserId, DateTime? fromDate, DateTime? toDate, int page = 1)
    {
        var model = await _auditLogService.GetLogsPagedAsync(action, adminUserId, fromDate, toDate, page, 25);
        return View(model);
    }

    // --- Permissions Management ---
    [HttpGet]
    [Route(UrlRewrite.Admin.Permissions)]
    [HasPermission(AppPermissions.Admin.PermissionsManage)]
    public async Task<IActionResult> Permissions()
    {
        var groups = await _permissionService.GetGroupsAsync();
        var permissions = await _permissionService.GetAllPermissionsAsync();

        ViewBag.Groups = groups;
        ViewBag.Permissions = permissions;
        return View();
    }

    [HttpPost]
    [Route(UrlRewrite.Admin.PermissionAssignGroup)]
    [HasPermission(AppPermissions.Admin.PermissionsManage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignGroup(int userId, string groupName)
    {
        var result = await _permissionService.AssignGroupToUserAsync(userId, groupName);
        var adminId = GetCurrentAdminId();

        await _auditLogService.LogAsync(adminId, "Permission.GroupAssigned", "UserPermission", userId.ToString(), $"Assigned to group: {groupName}", GetClientIp());

        if (result.Success)
            TempData["SuccessMessage"] = $"Group '{groupName}' assigned to user {userId}.";
        else
            TempData["ErrorMessage"] = result.ErrorMessage;

        return RedirectToAction(nameof(StudentDetails), new { id = userId });
    }

    [HttpPost]
    [Route(UrlRewrite.Admin.PermissionDirectOverride)]
    [HasPermission(AppPermissions.Admin.PermissionsManage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DirectOverride(int userId, string permissionName, bool isGranted)
    {
        var result = isGranted
            ? await _permissionService.GrantDirectPermissionAsync(userId, permissionName)
            : await _permissionService.RevokeDirectPermissionAsync(userId, permissionName);

        var adminId = GetCurrentAdminId();
        await _auditLogService.LogAsync(adminId, isGranted ? "Permission.DirectGrant" : "Permission.DirectRevocation", "UserPermission", userId.ToString(), $"Permission: {permissionName}", GetClientIp());

        if (result.Success)
            TempData["SuccessMessage"] = $"Direct permission override '{permissionName}' ({isGranted}) set for user {userId}.";
        else
            TempData["ErrorMessage"] = result.ErrorMessage;

        return RedirectToAction(nameof(StudentDetails), new { id = userId });
    }
}
