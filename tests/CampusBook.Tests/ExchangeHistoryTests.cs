using CampusBook.DataAccess;
using CampusBook.DataAccess.Implementation;
using CampusBook.Entities;
using CampusBook.Facade.Implementation;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CampusBook.Tests;

public class ExchangeHistoryTests
{
    private (ExchangeHistoryFacade facade, ApplicationDbContext context, User user1, User user2) SetupEnvironment(string dbName)
    {
        var context = TestDbContextFactory.Create(dbName);
        var reqRepo = new BookRequestRepository(context);
        var logger = NullLogger<ExchangeHistoryFacade>.Instance;

        var user1 = new User { UserId = 1, FullName = "Student Alice", StudentId = "STU-H1", Email = "alice@gmail.com", PasswordHash = "hash", RoleId = 1, IsActive = true };
        var user2 = new User { UserId = 2, FullName = "Student Bob", StudentId = "STU-H2", Email = "bob@gmail.com", PasswordHash = "hash", RoleId = 1, IsActive = true };
        context.Users.AddRange(user1, user2);

        // Transaction 1: Alice sold a book to Bob
        var saleBook = new Book { BookId = 1, Title = "Data Structures in C++", Author = "D.S. Malik", CategoryId = 1, Condition = BookCondition.Good, ExchangeType = ExchangeType.Sale, Price = 350, Description = "Desc", OwnerId = 1, Status = BookStatus.Completed };
        var saleReq = new BookRequest
        {
            RequestId = 1,
            BookId = 1,
            RequesterId = 2,
            Status = RequestStatus.Completed,
            OwnerConfirmed = true,
            RequesterConfirmed = true,
            RequesterConfirmedAt = DateTime.UtcNow.AddDays(-2),
            UpdatedAt = DateTime.UtcNow.AddDays(-2)
        };

        // Transaction 2: Bob exchanged a book with Alice
        var exchBook = new Book { BookId = 2, Title = "Database Systems", Author = "Navathe", CategoryId = 1, Condition = BookCondition.Good, ExchangeType = ExchangeType.Exchange, Price = 0, Description = "Desc", OwnerId = 2, Status = BookStatus.Completed };
        var exchReq = new BookRequest
        {
            RequestId = 2,
            BookId = 2,
            RequesterId = 1,
            Status = RequestStatus.Completed,
            OwnerConfirmed = true,
            RequesterConfirmed = true,
            RequesterConfirmedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1),
            ExchangeOffer = new ExchangeOffer
            {
                ExchangeOfferId = 1,
                RequestId = 2,
                Title = "Discrete Mathematics",
                Author = "Kenneth Rosen",
                CategoryId = 1,
                Condition = BookCondition.Good,
                Description = "Rosen 7th Edition"
            }
        };

        context.Books.AddRange(saleBook, exchBook);
        context.BookRequests.AddRange(saleReq, exchReq);
        context.SaveChanges();

        var facade = new ExchangeHistoryFacade(reqRepo, logger);
        return (facade, context, user1, user2);
    }

    [Fact]
    public async Task GetUserExchangeHistoryAsync_AliceHistory_ContainsSoldAndExchanged()
    {
        // Arrange
        var (facade, _, user1, _) = SetupEnvironment(Guid.NewGuid().ToString());

        // Act
        var history = await facade.GetUserExchangeHistoryAsync(user1.UserId);

        // Assert
        Assert.NotNull(history);
        Assert.Equal(2, history.Transactions.Count());
        Assert.Equal(1, history.TotalSold);
        Assert.Equal(0, history.TotalPurchased);
        Assert.Equal(1, history.TotalExchanged);

        // Verify transaction details
        var soldItem = history.Transactions.FirstOrDefault(t => t.WasOwner);
        Assert.NotNull(soldItem);
        Assert.Equal("Data Structures in C++", soldItem.BookTitle);
        Assert.Equal(350m, soldItem.Price);

        var exchangedItem = history.Transactions.FirstOrDefault(t => t.ExchangeType == ExchangeType.Exchange);
        Assert.NotNull(exchangedItem);
        Assert.Equal("Database Systems", exchangedItem.BookTitle);
        Assert.NotNull(exchangedItem.OfferedBookDetails);
        Assert.Equal("Discrete Mathematics", exchangedItem.OfferedBookDetails.Title);
        Assert.Equal("Kenneth Rosen", exchangedItem.OfferedBookDetails.Author);
    }

    [Fact]
    public async Task GetUserExchangeHistoryAsync_BobHistory_ContainsPurchasedAndExchanged()
    {
        // Arrange
        var (facade, _, _, user2) = SetupEnvironment(Guid.NewGuid().ToString());

        // Act
        var history = await facade.GetUserExchangeHistoryAsync(user2.UserId);

        // Assert
        Assert.NotNull(history);
        Assert.Equal(2, history.Transactions.Count());
        Assert.Equal(0, history.TotalSold);
        Assert.Equal(1, history.TotalPurchased);
        Assert.Equal(1, history.TotalExchanged);
    }
}
