namespace CampusBook.Framework;

public static class Permissions
{
    // Module: Books
    public static class Books
    {
        public const string View = "Books.View";
        public const string Create = "Books.Create";
        public const string EditOwn = "Books.EditOwn";
        public const string DeleteOwn = "Books.DeleteOwn";
        public const string EditAny = "Books.EditAny";
        public const string DeleteAny = "Books.DeleteAny";
    }

    // Module: Requests
    public static class Requests
    {
        public const string ViewOwn = "Requests.ViewOwn";
        public const string Create = "Requests.Create";
        public const string ManageOwn = "Requests.ManageOwn";
        public const string ManageAny = "Requests.ManageAny";
    }

    // Module: Wishlist
    public static class Wishlist
    {
        public const string ViewOwn = "Wishlist.ViewOwn";
        public const string ManageOwn = "Wishlist.ManageOwn";
    }

    // Module: WantedBooks
    public static class WantedBooks
    {
        public const string View = "WantedBooks.View";
        public const string Create = "WantedBooks.Create";
        public const string EditOwn = "WantedBooks.EditOwn";
        public const string DeleteOwn = "WantedBooks.DeleteOwn";
        public const string ManageAny = "WantedBooks.ManageAny";
    }

    // Module: Messaging
    public static class Messaging
    {
        public const string ViewOwn = "Messaging.ViewOwn";
        public const string Send = "Messaging.Send";
    }

    // Module: Notifications
    public static class Notifications
    {
        public const string ViewOwn = "Notifications.ViewOwn";
        public const string ManageOwn = "Notifications.ManageOwn";
    }

    // Module: History
    public static class History
    {
        public const string ViewOwn = "History.ViewOwn";
    }

    // Module: Profile
    public static class Profile
    {
        public const string ViewOwn = "Profile.ViewOwn";
        public const string EditOwn = "Profile.EditOwn";
        public const string ViewPublic = "Profile.ViewPublic";
    }

    // Module: Administration
    public static class Admin
    {
        public const string UsersView = "Admin.Users.View";
        public const string UsersManage = "Admin.Users.Manage";
        public const string PermissionsView = "Admin.Permissions.View";
        public const string PermissionsManage = "Admin.Permissions.Manage";
    }

    // Group Names
    public static class Groups
    {
        public const string Student = "Student";
        public const string Admin = "Admin";
        public const string Moderator = "Moderator";
    }
}
