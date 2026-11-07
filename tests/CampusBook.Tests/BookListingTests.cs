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

public class BookListingTests
{
    private (BookFacade facade, ApplicationDbContext context, User owner, User otherUser) SetupBookEnvironment(string dbName)
    {
        var context = TestDbContextFactory.Create(dbName);
        var bookRepo = new BookRepository(context);
        var requestRepo = new BookRequestRepository(context);
        var lookupRepo = new LookupRepository(context);
        var fileStorageMock = new Mock<IFileStorageService>();
        var logger = NullLogger<BookFacade>.Instance;

        var owner = new User
        {
            UserId = 1,
            FullName = "Owner Student",
            StudentId = "STU-2001",
            Email = "owner@university.edu",
            PasswordHash = "hash",
            RoleId = 1,
            IsEmailConfirmed = true
        };

        var otherUser = new User
        {
            UserId = 2,
            FullName = "Other Student",
            StudentId = "STU-2002",
            Email = "other@university.edu",
            PasswordHash = "hash",
            RoleId = 1,
            IsEmailConfirmed = true
        };

        context.Users.AddRange(owner, otherUser);
        context.SaveChanges();

        var facade = new BookFacade(bookRepo, requestRepo, lookupRepo, fileStorageMock.Object, logger);
        return (facade, context, owner, otherUser);
    }

    [Fact]
    public async Task CreateBookAsync_DonationType_ForcesPriceToZero()
    {
        // Arrange
        var (facade, context, owner, _) = SetupBookEnvironment(Guid.NewGuid().ToString());
        var model = new BookCreateViewModel
        {
            Title = "Calculus Early Transcendentals",
            Author = "James Stewart",
            CategoryId = 1,
            Condition = BookCondition.Good,
            ExchangeType = ExchangeType.Donation,
            Price = 25.00m, // User attempted to enter a price on a donation
            Description = "Giving away to any 1st year engineering student."
        };

        // Act
        var result = await facade.CreateBookAsync(owner.UserId, model);

        // Assert
        Assert.True(result.Success);
        var book = await context.Books.FindAsync(result.Data);
        Assert.NotNull(book);
        Assert.Equal(0.00m, book.Price); // Business rule: Donation is strictly free!
        Assert.Equal(BookStatus.Available, book.Status);
    }

    [Fact]
    public async Task CreateBookAsync_SaleType_WithNegativePrice_IsRejected()
    {
        // Arrange
        var (facade, _, owner, _) = SetupBookEnvironment(Guid.NewGuid().ToString());
        var model = new BookCreateViewModel
        {
            Title = "Operating System Concepts",
            Author = "Silberschatz",
            CategoryId = 1,
            Condition = BookCondition.Fair,
            ExchangeType = ExchangeType.Sale,
            Price = -10.00m, // Negative price!
            Description = "Great textbook."
        };

        // Act
        var result = await facade.CreateBookAsync(owner.UserId, model);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("cannot be negative", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateBookAsync_ByNonOwner_IsRejected()
    {
        // Arrange
        var (facade, context, owner, otherUser) = SetupBookEnvironment(Guid.NewGuid().ToString());
        var book = new Book
        {
            Title = "Original Title",
            Author = "Original Author",
            CategoryId = 1,
            Condition = BookCondition.Good,
            ExchangeType = ExchangeType.Sale,
            Price = 15.00m,
            Description = "Original description",
            OwnerId = owner.UserId,
            Status = BookStatus.Available
        };
        context.Books.Add(book);
        await context.SaveChangesAsync();

        var editModel = new BookEditViewModel
        {
            BookId = book.BookId,
            Title = "Hacked Title",
            Author = "Hacked Author",
            CategoryId = 1,
            Condition = BookCondition.Poor,
            ExchangeType = ExchangeType.Sale,
            Price = 1.00m,
            Description = "Hacked description"
        };

        // Act - other student tries to edit owner's book
        var result = await facade.UpdateBookAsync(otherUser.UserId, editModel);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("not authorized", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);

        // Confirm DB was untouched
        var freshBook = await context.Books.FindAsync(book.BookId);
        Assert.Equal("Original Title", freshBook!.Title);
    }

    [Fact]
    public async Task UpdateBookAsync_WhenBookIsReserved_IsRejected()
    {
        // Arrange
        var (facade, context, owner, _) = SetupBookEnvironment(Guid.NewGuid().ToString());
        var book = new Book
        {
            Title = "Reserved Book",
            Author = "Author",
            CategoryId = 1,
            Condition = BookCondition.Good,
            ExchangeType = ExchangeType.Sale,
            Price = 20.00m,
            Description = "In an active exchange",
            OwnerId = owner.UserId,
            Status = BookStatus.Reserved // Active exchange in progress!
        };
        context.Books.Add(book);
        await context.SaveChangesAsync();

        var editModel = new BookEditViewModel
        {
            BookId = book.BookId,
            Title = "Modified Reserved Title",
            Author = "Author",
            CategoryId = 1,
            Condition = BookCondition.Good,
            ExchangeType = ExchangeType.Sale,
            Price = 30.00m,
            Description = "Trying to change price while reserved"
        };

        // Act
        var result = await facade.UpdateBookAsync(owner.UserId, editModel);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("while it is reserved", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }
}
