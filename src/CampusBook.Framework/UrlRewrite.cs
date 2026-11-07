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
}

