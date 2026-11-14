using CampusBook.DataAccess;
using CampusBook.DataAccess.Implementation;
using CampusBook.Entities;
using CampusBook.Facade.Implementation;
using CampusBook.Facade.Services;
using CampusBook.Facade.ViewModels.Books;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace CampusBook.Tests;

public class BookCatalogueTests
{
    private (BookFacade facade, ApplicationDbContext context) SetupCatalogueEnvironment(string dbName)
    {
        var context = TestDbContextFactory.Create(dbName);
        var bookRepo = new BookRepository(context);
        var requestRepo = new BookRequestRepository(context);
        var lookupRepo = new LookupRepository(context);
        var wantedBookRepo = new WantedBookRepository(context);
        var notificationRepo = new NotificationRepository(context);
        var fileStorageMock = new Mock<IFileStorageService>();
        var logger = NullLogger<BookFacade>.Instance;

        var owner = new User
        {
            UserId = 1,
            FullName = "Jane Student",
            StudentId = "STU-3001",
            Email = "jane@university.edu",
            PasswordHash = "hash",
            RoleId = 1,
            IsEmailConfirmed = true
        };
        context.Users.Add(owner);

        // Seed diverse test books with various statuses
        var books = new List<Book>
        {
            new() { BookId = 1, Title = "Algorithms Unlocked", Author = "Thomas Cormen", Isbn = "978-0262518802", CategoryId = 1, Condition = BookCondition.New, ExchangeType = ExchangeType.Sale, Price = 15.00m, Description = "Desc", OwnerId = 1, Status = BookStatus.Available, CreatedAt = DateTime.UtcNow.AddDays(-5) },
            new() { BookId = 2, Title = "Clean Code", Author = "Robert Martin", Isbn = "978-0132350884", CategoryId = 1, Condition = BookCondition.Good, ExchangeType = ExchangeType.Sale, Price = 20.00m, Description = "Desc", OwnerId = 1, Status = BookStatus.Available, CreatedAt = DateTime.UtcNow.AddDays(-2) },
            new() { BookId = 3, Title = "Linear Algebra Done Right", Author = "Sheldon Axler", Isbn = "978-3319110790", CategoryId = 2, Condition = BookCondition.Fair, ExchangeType = ExchangeType.Exchange, Price = 0.00m, Description = "Desc", OwnerId = 1, Status = BookStatus.Available, CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new() { BookId = 4, Title = "Reserved Database Book", Author = "Ramakrishnan", CategoryId = 1, Condition = BookCondition.Good, ExchangeType = ExchangeType.Sale, Price = 25.00m, Description = "Desc", OwnerId = 1, Status = BookStatus.Reserved, CreatedAt = DateTime.UtcNow }, // Reserved!
            new() { BookId = 5, Title = "Completed Old Book", Author = "Old Author", CategoryId = 2, Condition = BookCondition.Poor, ExchangeType = ExchangeType.Sale, Price = 5.00m, Description = "Desc", OwnerId = 1, Status = BookStatus.Completed, CreatedAt = DateTime.UtcNow }, // Completed!
            new() { BookId = 6, Title = "Historical Donation Book", Author = "Legacy Author", CategoryId = 1, Condition = BookCondition.Fair, ExchangeType = ExchangeType.Donation, Price = 0.00m, Description = "Desc", OwnerId = 1, Status = BookStatus.Available, CreatedAt = DateTime.UtcNow.AddDays(-10) } // Donation (excluded from active catalogue)
        };

        context.Books.AddRange(books);
        context.SaveChanges();

        var facade = new BookFacade(bookRepo, requestRepo, lookupRepo, fileStorageMock.Object, wantedBookRepo, notificationRepo, logger);
        return (facade, context);
    }

    [Fact]
    public async Task GetCatalogueAsync_OnlyReturnsAvailableBooks()
    {
        // Arrange
        var (facade, _) = SetupCatalogueEnvironment(Guid.NewGuid().ToString());
        var filter = new BookFilterViewModel();

        // Act
        var result = await facade.GetCatalogueAsync(filter);

        // Assert
        Assert.NotNull(result.Books);
        Assert.Equal(3, result.Books.TotalCount); // Only 3 are Available! Reserved, Completed, and Donation excluded.
        Assert.DoesNotContain(result.Books.Items, b => b.Status != BookStatus.Available);
        Assert.DoesNotContain(result.Books.Items, b => b.ExchangeType == ExchangeType.Donation);
    }

    [Fact]
    public async Task GetCatalogueAsync_SearchByIsbn_ReturnsExactMatch()
    {
        // Arrange
        var (facade, _) = SetupCatalogueEnvironment(Guid.NewGuid().ToString());
        var filter = new BookFilterViewModel { SearchTerm = "978-0132350884" };

        // Act
        var result = await facade.GetCatalogueAsync(filter);

        // Assert
        Assert.Single(result.Books!.Items);
        Assert.Equal("Clean Code", result.Books.Items.First().Title);
    }

    [Fact]
    public async Task GetCatalogueAsync_FilterByCategory_ReturnsOnlyMatchingCategory()
    {
        // Arrange
        var (facade, _) = SetupCatalogueEnvironment(Guid.NewGuid().ToString());
        var filter = new BookFilterViewModel { CategoryId = 2 }; // Mathematics

        // Act
        var result = await facade.GetCatalogueAsync(filter);

        // Assert
        Assert.Single(result.Books!.Items);
        Assert.Equal("Linear Algebra Done Right", result.Books.Items.First().Title);
    }

    [Fact]
    public async Task GetCatalogueAsync_SortByPriceAscending_SortsCorrectly()
    {
        // Arrange
        var (facade, _) = SetupCatalogueEnvironment(Guid.NewGuid().ToString());
        var filter = new BookFilterViewModel { SortBy = "price_asc" };

        // Act
        var result = await facade.GetCatalogueAsync(filter);

        // Assert
        var prices = result.Books!.Items.Select(b => b.Price).ToList();
        Assert.Equal(new List<decimal> { 0.00m, 15.00m, 20.00m }, prices);
    }
}
