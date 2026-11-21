namespace CampusBook.Framework;

public static class UrlRewrite
{
    public static class Account
    {
        public const string Login = "login";
        public const string Register = "register";
        public const string Logout = "logout";
        public const string ConfirmEmail = "confirm-email";
        public const string ForgotPassword = "forgot-password";
        public const string ResetPassword = "reset-password";
        public const string AccessDenied = "access-denied";
    }

    public static class Profile
    {
        public const string MyProfile = "profile";
        public const string EditProfile = "profile/edit";
        public const string PublicProfile = "profile/user/{id}";
    }

    public static class Books
    {
        public const string Catalogue = "books";
        public const string Details = "books/details/{id}";
        public const string Create = "books/create";
        public const string MyBooks = "books/my-listings";
        public const string Edit = "books/edit/{id}";
        public const string Delete = "books/delete/{id}";
    }

    public static class Requests
    {
        public const string Sent = "requests/sent";
        public const string Received = "requests/received";
        public const string Create = "requests/create/{bookId}";
        public const string Details = "requests/details/{id}";
    }

    public static class Dashboard
    {
        public const string Index = "dashboard";
    }

    public static class Wishlist
    {
        public const string Index = "wishlist";
        public const string Toggle = "wishlist/toggle";
        public const string Remove = "wishlist/remove/{bookId}";
    }

    public static class WantedBooks
    {
        public const string Index = "wanted-books";
        public const string MyPosts = "wanted-books/my-posts";
        public const string Create = "wanted-books/create";
        public const string Edit = "wanted-books/edit/{id}";
        public const string Close = "wanted-books/close/{id}";
        public const string Details = "wanted-books/details/{id}";
    }

    public static class Notifications
    {
        public const string Index = "notifications";
        public const string MarkAsRead = "notifications/read/{id}";
        public const string MarkAllAsRead = "notifications/read-all";
    }

    public static class Messages
    {
        public const string Index = "messages";
        public const string Conversation = "messages/conversation/{id}";
        public const string Send = "messages/send";
    }

    public static class History
    {
        public const string Index = "history";
    }

    public static class Reports
    {
        public const string Create = "reports/create";
    }

    public static class Admin
    {
        public const string Dashboard = "admin";
        public const string Students = "admin/students";
        public const string StudentDetails = "admin/students/details/{id}";
        public const string StudentVerify = "admin/students/verify/{id}";
        public const string StudentRejectVerification = "admin/students/reject-verification/{id}";
        public const string StudentSuspend = "admin/students/suspend/{id}";
        public const string StudentReactivate = "admin/students/reactivate/{id}";
        public const string Books = "admin/books";
        public const string BookDetails = "admin/books/details/{id}";
        public const string BookApprove = "admin/books/approve/{id}";
        public const string BookReject = "admin/books/reject/{id}";
        public const string BookArchive = "admin/books/archive/{id}";
        public const string Categories = "admin/categories";
        public const string CategoryCreate = "admin/categories/create";
        public const string CategoryEdit = "admin/categories/edit/{id}";
        public const string CategoryToggle = "admin/categories/toggle/{id}";
        public const string Departments = "admin/departments";
        public const string DepartmentCreate = "admin/departments/create";
        public const string DepartmentEdit = "admin/departments/edit/{id}";
        public const string Courses = "admin/courses";
        public const string CourseCreate = "admin/courses/create";
        public const string CourseEdit = "admin/courses/edit/{id}";
        public const string CourseToggle = "admin/courses/toggle/{id}";
        public const string Requests = "admin/requests";
        public const string RequestDetails = "admin/requests/details/{id}";
        public const string RequestIntervene = "admin/requests/intervene/{id}";
        public const string Exchanges = "admin/exchanges";
        public const string Reports = "admin/reports";
        public const string ReportDetails = "admin/reports/details/{id}";
        public const string ReportResolve = "admin/reports/resolve/{id}";
        public const string ReportDismiss = "admin/reports/dismiss/{id}";
        public const string Reviews = "admin/reviews";
        public const string ReviewHide = "admin/reviews/hide/{id}";
        public const string ReviewRestore = "admin/reviews/restore/{id}";
        public const string Analytics = "admin/analytics";
        public const string AnalyticsExport = "admin/analytics/export";
        public const string AuditLogs = "admin/audit-logs";
        public const string Permissions = "admin/permissions";
        public const string PermissionAssignGroup = "admin/permissions/assign-group";
        public const string PermissionDirectOverride = "admin/permissions/direct-override";
    }
}

