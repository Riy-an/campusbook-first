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

        // Seed initial lookup data
        SeedLookupData(modelBuilder);
    }

    private static void SeedLookupData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().HasData(
            new Role { RoleId = 1, RoleName = "Student" },
            new Role { RoleId = 2, RoleName = "Admin" }
        );

        modelBuilder.Entity<Department>().HasData(
            new Department { DepartmentId = 1, Name = "Computer Science & Engineering", Code = "CSE" },
            new Department { DepartmentId = 2, Name = "Electrical & Electronic Engineering", Code = "EEE" },
            new Department { DepartmentId = 3, Name = "Business Administration", Code = "BBA" },
            new Department { DepartmentId = 4, Name = "Economics", Code = "ECON" },
            new Department { DepartmentId = 5, Name = "English Literature & Linguistics", Code = "ENG" },
            new Department { DepartmentId = 6, Name = "Mathematics & Natural Sciences", Code = "MNS" }
        );

        modelBuilder.Entity<BookCategory>().HasData(
            new BookCategory { CategoryId = 1, Name = "Computer Science & IT", Description = "Programming, Algorithms, AI, Networking, Databases" },
            new BookCategory { CategoryId = 2, Name = "Mathematics & Statistics", Description = "Calculus, Linear Algebra, Discrete Math, Statistics" },
            new BookCategory { CategoryId = 3, Name = "Engineering", Description = "Circuit Analysis, Mechanics, Thermodynamics, Materials" },
            new BookCategory { CategoryId = 4, Name = "Business & Economics", Description = "Accounting, Finance, Marketing, Microeconomics, Management" },
            new BookCategory { CategoryId = 5, Name = "Natural Sciences", Description = "Physics, Chemistry, Biology, Environmental Sciences" },
            new BookCategory { CategoryId = 6, Name = "Humanities & Social Sciences", Description = "History, Philosophy, Sociology, Psychology" },
            new BookCategory { CategoryId = 7, Name = "Literature & Languages", Description = "English, Fiction, Writing, Grammar, Foreign Languages" }
        );
    }
}

