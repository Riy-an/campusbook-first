using CampusBook.Entities;
using Microsoft.EntityFrameworkCore;

namespace CampusBook.DataAccess;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<StudentProfile> StudentProfiles => Set<StudentProfile>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<BookCategory> BookCategories => Set<BookCategory>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<BookRequest> BookRequests => Set<BookRequest>();
    public DbSet<ExchangeOffer> ExchangeOffers => Set<ExchangeOffer>();
    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
    public DbSet<WantedBook> WantedBooks => Set<WantedBook>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<PermissionGroup> PermissionGroups => Set<PermissionGroup>();
    public DbSet<PermissionGroupMap> PermissionGroupMaps => Set<PermissionGroupMap>();
    public DbSet<UserPermission> UserPermissions => Set<UserPermission>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<ExchangeReview> ExchangeReviews => Set<ExchangeReview>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.ConfigureWarnings(warnings =>
            warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Role configuration
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");
            entity.HasKey(r => r.RoleId);
            entity.Property(r => r.RoleName).IsRequired().HasMaxLength(50);
            entity.HasIndex(r => r.RoleName).IsUnique();
        });

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(u => u.UserId);
            entity.Property(u => u.FullName).IsRequired().HasMaxLength(150);
            entity.Property(u => u.StudentId).IsRequired().HasMaxLength(50);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(150);
            entity.Property(u => u.PasswordHash).IsRequired().HasMaxLength(500);
            entity.Property(u => u.EmailConfirmationToken).HasMaxLength(256);
            entity.Property(u => u.PasswordResetToken).HasMaxLength(256);

            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasIndex(u => u.StudentId).IsUnique();

            entity.HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // StudentProfile configuration (1:1 with User)
        modelBuilder.Entity<StudentProfile>(entity =>
        {
            entity.ToTable("StudentProfiles");
            entity.HasKey(sp => sp.UserId);
            entity.Property(sp => sp.Department).IsRequired().HasMaxLength(100);
            entity.Property(sp => sp.Batch).IsRequired().HasMaxLength(50);
            entity.Property(sp => sp.UniversityEmail).HasMaxLength(150);
            entity.Property(sp => sp.Semester).HasMaxLength(50);
            entity.Property(sp => sp.PhoneNumber).HasMaxLength(25);
            entity.Property(sp => sp.ProfilePictureUrl).HasMaxLength(300);
            entity.Property(sp => sp.StudentIdCardUrl).HasMaxLength(300);
            entity.Property(sp => sp.IdCardChangeCount).HasDefaultValue(0);
            entity.Property(sp => sp.Bio).HasMaxLength(1000);

            entity.HasOne(sp => sp.User)
                .WithOne(u => u.StudentProfile)
                .HasForeignKey<StudentProfile>(sp => sp.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Department configuration
        modelBuilder.Entity<Department>(entity =>
        {
            entity.ToTable("Departments");
            entity.HasKey(d => d.DepartmentId);
            entity.Property(d => d.Name).IsRequired().HasMaxLength(100);
            entity.Property(d => d.Code).IsRequired().HasMaxLength(20);
            entity.HasIndex(d => d.Code).IsUnique();
        });

        // BookCategory configuration
        modelBuilder.Entity<BookCategory>(entity =>
        {
            entity.ToTable("BookCategories");
            entity.HasKey(c => c.CategoryId);
            entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
            entity.Property(c => c.Description).HasMaxLength(250);
            entity.HasIndex(c => c.Name).IsUnique();
        });

        // Book configuration
        modelBuilder.Entity<Book>(entity =>
        {
            entity.ToTable("Books");
            entity.HasKey(b => b.BookId);
            entity.Property(b => b.Title).IsRequired().HasMaxLength(250);
            entity.Property(b => b.Author).IsRequired().HasMaxLength(200);
            entity.Property(b => b.Isbn).HasMaxLength(50);
            entity.Property(b => b.Edition).HasMaxLength(50);
            entity.Property(b => b.CourseCode).HasMaxLength(50);
            entity.Property(b => b.Price).HasPrecision(18, 2);
            entity.Property(b => b.Description).IsRequired().HasMaxLength(2500);
            entity.Property(b => b.ImageUrl).HasMaxLength(300);

            entity.HasIndex(b => b.Title);
            entity.HasIndex(b => b.Author);
            entity.HasIndex(b => b.Isbn);
            entity.HasIndex(b => new { b.Status, b.CategoryId });

            entity.HasOne(b => b.Category)
                .WithMany(c => c.Books)
                .HasForeignKey(b => b.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(b => b.Department)
                .WithMany(d => d.Books)
                .HasForeignKey(b => b.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(b => b.Owner)
                .WithMany(u => u.Books)
                .HasForeignKey(b => b.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // BookRequest configuration
        modelBuilder.Entity<BookRequest>(entity =>
        {
            entity.ToTable("BookRequests");
            entity.HasKey(br => br.RequestId);
            entity.Property(br => br.RequestMessage).HasMaxLength(1000);

            entity.HasIndex(br => new { br.BookId, br.RequesterId, br.Status });

            // Requested book relationship (Restrict delete behavior to prevent cycles)
            entity.HasOne(br => br.Book)
                .WithMany(b => b.Requests)
                .HasForeignKey(br => br.BookId)
                .OnDelete(DeleteBehavior.Restrict);

            // Requester relationship (Restrict delete behavior)
            entity.HasOne(br => br.Requester)
                .WithMany(u => u.SentRequests)
                .HasForeignKey(br => br.RequesterId)
                .OnDelete(DeleteBehavior.Restrict);

            // Offered book for exchange relationship (Restrict delete behavior)
            entity.HasOne(br => br.OfferedBook)
                .WithMany()
                .HasForeignKey(br => br.OfferedBookId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ExchangeOffer configuration (1:1 with BookRequest)
        modelBuilder.Entity<ExchangeOffer>(entity =>
        {
            entity.ToTable("ExchangeOffers");
            entity.HasKey(eo => eo.ExchangeOfferId);
            entity.Property(eo => eo.Title).IsRequired().HasMaxLength(250);
            entity.Property(eo => eo.Author).IsRequired().HasMaxLength(200);
            entity.Property(eo => eo.Isbn).HasMaxLength(50);
            entity.Property(eo => eo.Edition).HasMaxLength(50);
            entity.Property(eo => eo.PublicationYear).HasMaxLength(20);
            entity.Property(eo => eo.CourseCode).HasMaxLength(50);
            entity.Property(eo => eo.Description).IsRequired().HasMaxLength(2500);
            entity.Property(eo => eo.Notes).HasMaxLength(1000);
            entity.Property(eo => eo.ImageUrl).HasMaxLength(300);

            entity.HasIndex(eo => eo.RequestId).IsUnique();

            entity.HasOne(eo => eo.Request)
                .WithOne(r => r.ExchangeOffer)
                .HasForeignKey<ExchangeOffer>(eo => eo.RequestId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(eo => eo.Category)
                .WithMany()
                .HasForeignKey(eo => eo.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(eo => eo.Department)
                .WithMany()
                .HasForeignKey(eo => eo.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(eo => eo.OfferedBookListing)
                .WithMany(b => b.ExchangeOffersAsOfferedListing)
                .HasForeignKey(eo => eo.OfferedBookListingId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // WishlistItem configuration (unique per User and Book)
        modelBuilder.Entity<WishlistItem>(entity =>
        {
            entity.ToTable("WishlistItems");
            entity.HasKey(w => w.WishlistItemId);

            entity.HasIndex(w => new { w.UserId, w.BookId }).IsUnique();

            entity.HasOne(w => w.User)
                .WithMany(u => u.WishlistItems)
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(w => w.Book)
                .WithMany(b => b.WishlistItems)
                .HasForeignKey(w => w.BookId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // WantedBook configuration
        modelBuilder.Entity<WantedBook>(entity =>
        {
            entity.ToTable("WantedBooks");
            entity.HasKey(wb => wb.WantedBookId);
            entity.Property(wb => wb.Title).IsRequired().HasMaxLength(250);
            entity.Property(wb => wb.Author).HasMaxLength(200);
            entity.Property(wb => wb.Isbn).HasMaxLength(50);
            entity.Property(wb => wb.CourseCode).HasMaxLength(50);
            entity.Property(wb => wb.MaxBudget).HasPrecision(18, 2);
            entity.Property(wb => wb.Description).HasMaxLength(1500);

            entity.HasIndex(wb => new { wb.UserId, wb.Status });
            entity.HasIndex(wb => wb.Title);
            entity.HasIndex(wb => wb.Isbn);

            entity.HasOne(wb => wb.User)
                .WithMany(u => u.WantedBooks)
                .HasForeignKey(wb => wb.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(wb => wb.Category)
                .WithMany()
                .HasForeignKey(wb => wb.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(wb => wb.Department)
                .WithMany()
                .HasForeignKey(wb => wb.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Notification configuration
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notifications");
            entity.HasKey(n => n.NotificationId);
            entity.Property(n => n.Title).IsRequired().HasMaxLength(150);
            entity.Property(n => n.Message).IsRequired().HasMaxLength(500);
            entity.Property(n => n.RelatedEntityType).HasMaxLength(50);
            entity.Property(n => n.NavigationUrl).HasMaxLength(250);

            entity.HasIndex(n => new { n.UserId, n.IsRead });
            entity.HasIndex(n => n.CreatedAt);

            entity.HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Conversation configuration (1:1 with BookRequest)
        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.ToTable("Conversations");
            entity.HasKey(c => c.ConversationId);

            entity.HasIndex(c => c.RequestId).IsUnique();

            entity.HasOne(c => c.Request)
                .WithOne(r => r.Conversation)
                .HasForeignKey<Conversation>(c => c.RequestId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Message configuration
        modelBuilder.Entity<Message>(entity =>
        {
            entity.ToTable("Messages");
            entity.HasKey(m => m.MessageId);
            entity.Property(m => m.Content).IsRequired().HasMaxLength(2000);

            entity.HasIndex(m => new { m.ConversationId, m.SentAt });

            entity.HasOne(m => m.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(m => m.Sender)
                .WithMany(u => u.SentMessages)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Permission configuration
        modelBuilder.Entity<Permission>(entity =>
        {
            entity.ToTable("Permissions");
            entity.HasKey(p => p.PermissionId);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(100);
            entity.Property(p => p.Module).IsRequired().HasMaxLength(50);
            entity.Property(p => p.Description).IsRequired().HasMaxLength(250);
            entity.HasIndex(p => p.Name).IsUnique();
        });

        // PermissionGroup configuration
        modelBuilder.Entity<PermissionGroup>(entity =>
        {
            entity.ToTable("PermissionGroups");
            entity.HasKey(pg => pg.PermissionGroupId);
            entity.Property(pg => pg.GroupName).IsRequired().HasMaxLength(50);
            entity.Property(pg => pg.Description).HasMaxLength(250);
            entity.HasIndex(pg => pg.GroupName).IsUnique();
        });

        // PermissionGroupMap configuration
        modelBuilder.Entity<PermissionGroupMap>(entity =>
        {
            entity.ToTable("PermissionGroupMaps");
            entity.HasKey(pgm => new { pgm.PermissionGroupId, pgm.PermissionId });

            entity.HasOne(pgm => pgm.PermissionGroup)
                .WithMany(pg => pg.GroupMaps)
                .HasForeignKey(pgm => pgm.PermissionGroupId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(pgm => pgm.Permission)
                .WithMany(p => p.GroupMaps)
                .HasForeignKey(pgm => pgm.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // UserPermission configuration
        modelBuilder.Entity<UserPermission>(entity =>
        {
            entity.ToTable("UserPermissions");
            entity.HasKey(up => up.UserPermissionId);

            entity.HasOne(up => up.User)
                .WithMany(u => u.UserPermissions)
                .HasForeignKey(up => up.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(up => up.PermissionGroup)
                .WithMany(pg => pg.UserPermissions)
                .HasForeignKey(up => up.PermissionGroupId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(up => up.Permission)
                .WithMany(p => p.UserPermissions)
                .HasForeignKey(up => up.PermissionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(up => new { up.UserId, up.PermissionGroupId })
                .HasFilter("[PermissionGroupId] IS NOT NULL")
                .IsUnique();

            entity.HasIndex(up => new { up.UserId, up.PermissionId })
                .HasFilter("[PermissionId] IS NOT NULL")
                .IsUnique();
        });

        // Course configuration
        modelBuilder.Entity<Course>(entity =>
        {
            entity.ToTable("Courses");
            entity.HasKey(c => c.CourseId);
            entity.Property(c => c.CourseCode).IsRequired().HasMaxLength(20);
            entity.Property(c => c.CourseName).IsRequired().HasMaxLength(150);
            entity.HasIndex(c => new { c.DepartmentId, c.CourseCode }).IsUnique();
            entity.HasOne(c => c.Department)
                .WithMany(d => d.Courses)
                .HasForeignKey(c => c.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Report configuration
        modelBuilder.Entity<Report>(entity =>
        {
            entity.ToTable("Reports");
            entity.HasKey(r => r.ReportId);
            entity.Property(r => r.Reason).IsRequired().HasMaxLength(100);
            entity.Property(r => r.Description).IsRequired().HasMaxLength(2000);
            entity.Property(r => r.ResolutionNotes).HasMaxLength(2000);
            entity.HasIndex(r => r.Status);
            entity.HasIndex(r => new { r.TargetType, r.TargetId });
            entity.HasOne(r => r.Reporter)
                .WithMany(u => u.ReportsSubmitted)
                .HasForeignKey(r => r.ReporterId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(r => r.AssignedAdmin)
                .WithMany(u => u.ReportsAssigned)
                .HasForeignKey(r => r.AssignedAdminId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ExchangeReview configuration
        modelBuilder.Entity<ExchangeReview>(entity =>
        {
            entity.ToTable("ExchangeReviews");
            entity.HasKey(r => r.ReviewId);
            entity.Property(r => r.Comment).IsRequired().HasMaxLength(2000);
            entity.Property(r => r.ModerationReason).HasMaxLength(500);
            entity.HasIndex(r => r.RequestId);
            entity.HasOne(r => r.Request)
                .WithMany(req => req.Reviews)
                .HasForeignKey(r => r.RequestId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(r => r.Reviewer)
                .WithMany(u => u.ReviewsGiven)
                .HasForeignKey(r => r.ReviewerId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(r => r.ReviewedUser)
                .WithMany(u => u.ReviewsReceived)
                .HasForeignKey(r => r.ReviewedUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // AuditLog configuration
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasKey(a => a.AuditLogId);
            entity.Property(a => a.Action).IsRequired().HasMaxLength(100);
            entity.Property(a => a.TargetEntityType).IsRequired().HasMaxLength(100);
            entity.Property(a => a.TargetId).HasMaxLength(100);
            entity.Property(a => a.Details).IsRequired().HasMaxLength(4000);
            entity.Property(a => a.IpAddress).HasMaxLength(50);
            entity.HasIndex(a => a.TimestampUtc);
            entity.HasIndex(a => a.Action);
            entity.HasOne(a => a.AdminUser)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(a => a.AdminUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Seed initial lookup data
        SeedLookupData(modelBuilder);
    }

    private static void SeedLookupData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().HasData(
            new Role { RoleId = 1, RoleName = "Student" },
            new Role { RoleId = 2, RoleName = "Admin" }
        );

        var seedTimestamp = new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<Department>().HasData(
            new Department { DepartmentId = 1, Name = "Computer Science & Engineering", Code = "CSE", IsActive = true },
            new Department { DepartmentId = 2, Name = "Electrical & Electronic Engineering", Code = "EEE", IsActive = true },
            new Department { DepartmentId = 3, Name = "Business Administration", Code = "BBA", IsActive = true },
            new Department { DepartmentId = 4, Name = "Economics", Code = "ECON", IsActive = true },
            new Department { DepartmentId = 5, Name = "English Literature & Linguistics", Code = "ENG", IsActive = true },
            new Department { DepartmentId = 6, Name = "Mathematics & Natural Sciences", Code = "MNS", IsActive = true }
        );

        modelBuilder.Entity<BookCategory>().HasData(
            new BookCategory { CategoryId = 1, Name = "Computer Science & IT", Description = "Programming, Algorithms, AI, Networking, Databases", IsActive = true, CreatedAt = seedTimestamp },
            new BookCategory { CategoryId = 2, Name = "Mathematics & Statistics", Description = "Calculus, Linear Algebra, Discrete Math, Statistics", IsActive = true, CreatedAt = seedTimestamp },
            new BookCategory { CategoryId = 3, Name = "Engineering", Description = "Circuit Analysis, Mechanics, Thermodynamics, Materials", IsActive = true, CreatedAt = seedTimestamp },
            new BookCategory { CategoryId = 4, Name = "Business & Economics", Description = "Accounting, Finance, Marketing, Microeconomics, Management", IsActive = true, CreatedAt = seedTimestamp },
            new BookCategory { CategoryId = 5, Name = "Natural Sciences", Description = "Physics, Chemistry, Biology, Environmental Sciences", IsActive = true, CreatedAt = seedTimestamp },
            new BookCategory { CategoryId = 6, Name = "Humanities & Social Sciences", Description = "History, Philosophy, Sociology, Psychology", IsActive = true, CreatedAt = seedTimestamp },
            new BookCategory { CategoryId = 7, Name = "Literature & Languages", Description = "English, Fiction, Writing, Grammar, Foreign Languages", IsActive = true, CreatedAt = seedTimestamp }
        );

        // Seed Permission Groups
        modelBuilder.Entity<PermissionGroup>().HasData(
            new PermissionGroup { PermissionGroupId = 1, GroupName = "Student", Description = "Standard student role with access to exchange, browsing, requests, wishlist, wanted posts, messaging, and profile management.", IsSystem = true, CreatedAt = seedTimestamp },
            new PermissionGroup { PermissionGroupId = 2, GroupName = "Admin", Description = "Full system administration with unrestricted access to all operations, user management, and permissions.", IsSystem = true, CreatedAt = seedTimestamp },
            new PermissionGroup { PermissionGroupId = 3, GroupName = "Moderator", Description = "Student moderator with elevated content moderation and user overview permissions.", IsSystem = false, CreatedAt = seedTimestamp }
        );

        // Seed Permissions
        var permissions = new[]
        {
            new Permission { PermissionId = 1, Name = "Books.View", Module = "Books", Description = "Browse and view available book listings", CreatedAt = seedTimestamp },
            new Permission { PermissionId = 2, Name = "Books.Create", Module = "Books", Description = "Publish new book listings for sale or exchange", CreatedAt = seedTimestamp },
            new Permission { PermissionId = 3, Name = "Books.EditOwn", Module = "Books", Description = "Edit own published book listings", CreatedAt = seedTimestamp },
            new Permission { PermissionId = 4, Name = "Books.DeleteOwn", Module = "Books", Description = "Deactivate or remove own book listings", CreatedAt = seedTimestamp },
            new Permission { PermissionId = 5, Name = "Books.EditAny", Module = "Books", Description = "Edit any book listing across the system (Moderator/Admin)", CreatedAt = seedTimestamp },
            new Permission { PermissionId = 6, Name = "Books.DeleteAny", Module = "Books", Description = "Deactivate any book listing (Moderator/Admin)", CreatedAt = seedTimestamp },
            new Permission { PermissionId = 7, Name = "Requests.ViewOwn", Module = "Requests", Description = "View sent and received book requests", CreatedAt = seedTimestamp },
            new Permission { PermissionId = 8, Name = "Requests.Create", Module = "Requests", Description = "Submit request for a book listing", CreatedAt = seedTimestamp },
            new Permission { PermissionId = 9, Name = "Requests.ManageOwn", Module = "Requests", Description = "Accept, reject, or cancel own requests and confirm handover", CreatedAt = seedTimestamp },
            new Permission { PermissionId = 10, Name = "Requests.ManageAny", Module = "Requests", Description = "Arbitrate or manage any request in the system", CreatedAt = seedTimestamp },
            new Permission { PermissionId = 11, Name = "Wishlist.ViewOwn", Module = "Wishlist", Description = "View personal book wishlist", CreatedAt = seedTimestamp },
            new Permission { PermissionId = 12, Name = "Wishlist.ManageOwn", Module = "Wishlist", Description = "Add or remove books from personal wishlist", CreatedAt = seedTimestamp },
            new Permission { PermissionId = 13, Name = "WantedBooks.View", Module = "WantedBooks", Description = "Browse and view student wanted-book posts", CreatedAt = seedTimestamp },
            new Permission { PermissionId = 14, Name = "WantedBooks.Create", Module = "WantedBooks", Description = "Post a request for a needed book", CreatedAt = seedTimestamp },
            new Permission { PermissionId = 15, Name = "WantedBooks.EditOwn", Module = "WantedBooks", Description = "Edit own wanted-book posts", CreatedAt = seedTimestamp },
            new Permission { PermissionId = 16, Name = "WantedBooks.DeleteOwn", Module = "WantedBooks", Description = "Close or remove own wanted-book posts", CreatedAt = seedTimestamp },
            new Permission { PermissionId = 17, Name = "WantedBooks.ManageAny", Module = "WantedBooks", Description = "Edit or close any wanted-book post in the system", CreatedAt = seedTimestamp },
            new Permission { PermissionId = 18, Name = "Messaging.ViewOwn", Module = "Messaging", Description = "View conversation threads for accepted requests", CreatedAt = seedTimestamp },
            new Permission { PermissionId = 19, Name = "Messaging.Send", Module = "Messaging", Description = "Send messages in accepted request conversations", CreatedAt = seedTimestamp },
            new Permission { PermissionId = 20, Name = "Notifications.ViewOwn", Module = "Notifications", Description = "View personal in-app notifications", CreatedAt = seedTimestamp },
            new Permission { PermissionId = 21, Name = "Notifications.ManageOwn", Module = "Notifications", Description = "Mark personal notifications as read or clear all", CreatedAt = seedTimestamp },
            new Permission { PermissionId = 22, Name = "History.ViewOwn", Module = "History", Description = "View personal completed exchange and transaction history", CreatedAt = seedTimestamp },
            new Permission { PermissionId = 23, Name = "Profile.ViewOwn", Module = "Profile", Description = "View own student profile details and ID card", CreatedAt = seedTimestamp },
            new Permission { PermissionId = 24, Name = "Profile.EditOwn", Module = "Profile", Description = "Edit personal profile details and upload ID card photo", CreatedAt = seedTimestamp },
            new Permission { PermissionId = 25, Name = "Profile.ViewPublic", Module = "Profile", Description = "View public peer student profile summaries", CreatedAt = seedTimestamp },
            new Permission { PermissionId = 26, Name = "Admin.Users.View", Module = "Administration", Description = "View registered student users and verification status", CreatedAt = seedTimestamp },
            new Permission { PermissionId = 27, Name = "Admin.Users.Manage", Module = "Administration", Description = "Activate, suspend, or update student accounts", CreatedAt = seedTimestamp },
            new Permission { PermissionId = 28, Name = "Admin.Permissions.View", Module = "Administration", Description = "View permission groups, assignments, and roles", CreatedAt = seedTimestamp },
            new Permission { PermissionId = 29, Name = "Admin.Permissions.Manage", Module = "Administration", Description = "Configure permissions, map groups, and assign user overrides", CreatedAt = seedTimestamp },
            new Permission { PermissionId = 30, Name = "Admin.Dashboard.View", Module = "Administration", Description = "View real-time database-backed admin metrics and dashboards", CreatedAt = seedTimestamp },
            new Permission { PermissionId = 31, Name = "Admin.Students.View", Module = "Administration", Description = "Search, filter, and view student accounts and verification status", CreatedAt = seedTimestamp },
            new Permission { PermissionId = 32, Name = "Admin.Students.Verify", Module = "Administration", Description = "Review university affiliation and verify or reject student ID cards", CreatedAt = seedTimestamp },
            new Permission { PermissionId = 33, Name = "Admin.Students.Suspend", Module = "Administration", Description = "Suspend or reactivate student accounts with reason recording", CreatedAt = seedTimestamp },
            new Permission { PermissionId = 34, Name = "Admin.Students.Manage", Module = "Administration", Description = "Comprehensive student account management and profile oversight", CreatedAt = seedTimestamp },
            new Permission { PermissionId = 35, Name = "Admin.Books.View", Module = "Administration", Description = "Inspect all student book listings across the platform", CreatedAt = seedTimestamp },
            new Permission { PermissionId = 36, Name = "Admin.Books.Moderate", Module = "Administration", Description = "Approve, reject, or archive inappropriate book listings", CreatedAt = seedTimestamp },
            new Permission { PermissionId = 37, Name = "Admin.Categories.Manage", Module = "Administration", Description = "Create, edit, and activate/deactivate categories and academic classifications", CreatedAt = seedTimestamp },
            new Permission { PermissionId = 38, Name = "Admin.Requests.View", Module = "Administration", Description = "Monitor book exchange requests, transactions, and reservation states", CreatedAt = seedTimestamp },
            new Permission { PermissionId = 39, Name = "Admin.Exchanges.View", Module = "Administration", Description = "Inspect trade offers, completed exchanges, and handover records", CreatedAt = seedTimestamp },
            new Permission { PermissionId = 40, Name = "Admin.Reports.Manage", Module = "Administration", Description = "Investigate user/listing complaints, resolve or dismiss reports", CreatedAt = seedTimestamp },
            new Permission { PermissionId = 41, Name = "Admin.Reviews.Moderate", Module = "Administration", Description = "Moderate, hide, or restore student feedback and exchange ratings", CreatedAt = seedTimestamp },
            new Permission { PermissionId = 42, Name = "Admin.Analytics.View", Module = "Administration", Description = "Access institutional analytics, trend breakdowns, and CSV reports", CreatedAt = seedTimestamp },
            new Permission { PermissionId = 43, Name = "Admin.AuditLogs.View", Module = "Administration", Description = "Inspect immutable security audit trails and administrative action history", CreatedAt = seedTimestamp }
        };
        modelBuilder.Entity<Permission>().HasData(permissions);

        // Seed Permission Group Maps
        // 1. Student permissions: 1, 2, 3, 4, 7, 8, 9, 11, 12, 13, 14, 15, 16, 18, 19, 20, 21, 22, 23, 24, 25
        var studentPermIds = new[] { 1, 2, 3, 4, 7, 8, 9, 11, 12, 13, 14, 15, 16, 18, 19, 20, 21, 22, 23, 24, 25 };
        var groupMaps = new List<PermissionGroupMap>();
        foreach (var pid in studentPermIds)
        {
            groupMaps.Add(new PermissionGroupMap { PermissionGroupId = 1, PermissionId = pid, CreatedAt = seedTimestamp });
        }

        // 2. Moderator permissions: All student permissions + 5, 6, 10, 17, 26, 35, 36, 40
        var modExtraIds = new[] { 5, 6, 10, 17, 26, 35, 36, 40 };
        foreach (var pid in studentPermIds.Concat(modExtraIds))
        {
            groupMaps.Add(new PermissionGroupMap { PermissionGroupId = 3, PermissionId = pid, CreatedAt = seedTimestamp });
        }

        // 3. Admin permissions: 1 through 43
        for (int pid = 1; pid <= 43; pid++)
        {
            groupMaps.Add(new PermissionGroupMap { PermissionGroupId = 2, PermissionId = pid, CreatedAt = seedTimestamp });
        }

        modelBuilder.Entity<PermissionGroupMap>().HasData(groupMaps);
    }
}

