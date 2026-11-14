using CampusBook.DataAccess;
using CampusBook.DataAccess.Implementation;
using CampusBook.Entities;
using CampusBook.Facade.Implementation;
using CampusBook.Facade.Services;
using CampusBook.Facade.ViewModels.Books;
using CampusBook.Facade.ViewModels.WantedBooks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace CampusBook.Tests;

public class WantedBookTests
{
    private (WantedBookFacade facade, BookFacade bookFacade, ApplicationDbContext context, User student1, User student2) SetupEnvironment(string dbName)
    {
        var context = TestDbContextFactory.Create(dbName);
        var wantedRepo = new WantedBookRepository(context);
        var bookRepo = new BookRepository(context);
        var requestRepo = new BookRequestRepository(context);
        var lookupRepo = new LookupRepository(context);
        var notifRepo = new NotificationRepository(context);
        var fileStorageMock = new Mock<IFileStorageService>();

        var student1 = new User { UserId = 1, FullName = "Student One", StudentId = "STU-W1", Email = "w1@gmail.com", PasswordHash = "hash", RoleId = 1, IsActive = true };
        var student2 = new User { UserId = 2, FullName = "Student Two", StudentId = "STU-W2", Email = "w2@gmail.com", PasswordHash = "hash", RoleId = 1, IsActive = true };
        context.Users.AddRange(student1, student2);
        context.SaveChanges();

        var wantedFacade = new WantedBookFacade(wantedRepo, lookupRepo, notifRepo, NullLogger<WantedBookFacade>.Instance);
        var bookFacade = new BookFacade(bookRepo, requestRepo, lookupRepo, fileStorageMock.Object, wantedRepo, notifRepo, NullLogger<BookFacade>.Instance);

        return (wantedFacade, bookFacade, context, student1, student2);
    }

    [Fact]
    public async Task CreateWantedBookAsync_ValidPost_Succeeds()
    {
        // Arrange
        var (wantedFacade, _, context, student1, _) = SetupEnvironment(Guid.NewGuid().ToString());
        var model = new WantedBookCreateViewModel
        {
            Title = "Introduction to Automata Theory",
            Author = "Hopcroft & Ullman",
            CategoryId = 1,
            CourseCode = "CSE320",
            MaxBudget = 300,
            PreferredExchangeType = ExchangeType.Sale,
            Description = "Needed urgently for midterm."
        };

        // Act
        var result = await wantedFacade.CreateWantedBookAsync(student1.UserId, model);

        // Assert
        Assert.True(result.Success);
        var created = await context.WantedBooks.FindAsync(result.Data);
        Assert.NotNull(created);
        Assert.Equal("Introduction to Automata Theory", created.Title);
        Assert.Equal(WantedBookStatus.Active, created.Status);
    }

    [Fact]
    public async Task UpdateWantedBookAsync_ByOwner_Succeeds_ByOther_Rejected()
    {
        // Arrange
        var (wantedFacade, _, context, student1, student2) = SetupEnvironment(Guid.NewGuid().ToString());
        var post = new WantedBook
        {
            UserId = student1.UserId,
            Title = "Original Wanted Title",
            Author = "Author",
            CategoryId = 1,
            Status = WantedBookStatus.Active,
            Description = "Initial"
        };
        context.WantedBooks.Add(post);
        await context.SaveChangesAsync();

        var editModel = new WantedBookEditViewModel
        {
            WantedBookId = post.WantedBookId,
            Title = "Updated Wanted Title",
            Author = "Author",
            CategoryId = 1,
            Description = "Updated description"
        };

        // Act 1: Student 2 tries to edit
        var forbiddenResult = await wantedFacade.UpdateWantedBookAsync(student2.UserId, editModel);
        Assert.False(forbiddenResult.Success);
        Assert.Contains("not authorized", forbiddenResult.ErrorMessage, StringComparison.OrdinalIgnoreCase);

        // Act 2: Student 1 edits their own
        var successResult = await wantedFacade.UpdateWantedBookAsync(student1.UserId, editModel);
        Assert.True(successResult.Success);

        var refreshed = await context.WantedBooks.FindAsync(post.WantedBookId);
        Assert.Equal("Updated Wanted Title", refreshed!.Title);
    }

    [Fact]
    public async Task CloseWantedBookAsync_ByOwner_MarksAsClosed()
    {
        // Arrange
        var (wantedFacade, _, context, student1, _) = SetupEnvironment(Guid.NewGuid().ToString());
        var post = new WantedBook
        {
            UserId = student1.UserId,
            Title = "Calculus",
            Author = "Stewart",
            CategoryId = 2,
            Status = WantedBookStatus.Active,
            Description = "Looking for calculus book"
        };
        context.WantedBooks.Add(post);
        await context.SaveChangesAsync();

        // Act
        var result = await wantedFacade.CloseWantedBookAsync(student1.UserId, post.WantedBookId);

        // Assert
        Assert.True(result.Success);
        var refreshed = await context.WantedBooks.FindAsync(post.WantedBookId);
        Assert.Equal(WantedBookStatus.Closed, refreshed!.Status);
    }

    [Fact]
    public async Task CreateBookAsync_AutomaticallyNotifiesUsersWithMatchingWantedBooks()
    {
        // Arrange
        var (wantedFacade, bookFacade, context, student1, student2) = SetupEnvironment(Guid.NewGuid().ToString());

        // Student 1 posted a wanted book for "Design Patterns"
        var wantedPost = new WantedBook
        {
            UserId = student1.UserId,
            Title = "Design Patterns: Elements of Reusable Object-Oriented Software",
            Author = "Gang of Four",
            CategoryId = 1,
            Status = WantedBookStatus.Active,
            Description = "Need GOF design patterns book"
        };
        context.WantedBooks.Add(wantedPost);
        await context.SaveChangesAsync();

        // Student 2 creates a listing that matches
        var bookModel = new BookCreateViewModel
        {
            Title = "Design Patterns",
            Author = "Erich Gamma, Richard Helm",
            CategoryId = 1,
            Condition = BookCondition.Good,
            ExchangeType = ExchangeType.Sale,
            Price = 350,
            Description = "GOF book in great condition"
        };

        // Act
        var result = await bookFacade.CreateBookAsync(student2.UserId, bookModel);

        // Assert
        Assert.True(result.Success);

        // Check that student 1 received a notification
        var notifications = context.Notifications.Where(n => n.UserId == student1.UserId).ToList();
        Assert.NotEmpty(notifications);
        Assert.Contains(notifications, n => n.Type == NotificationType.WantedBookMatch && n.Title.Contains("Book Match"));
    }
}
