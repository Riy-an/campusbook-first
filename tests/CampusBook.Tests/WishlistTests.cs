using CampusBook.DataAccess;
using CampusBook.DataAccess.Implementation;
using CampusBook.Entities;
using CampusBook.Facade.Implementation;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CampusBook.Tests;

public class WishlistTests
{
    private (WishlistFacade facade, ApplicationDbContext context, User user1, User user2, Book book1, Book book2) SetupEnvironment(string dbName)
    {
        var context = TestDbContextFactory.Create(dbName);
        var wishlistRepo = new WishlistRepository(context);
        var bookRepo = new BookRepository(context);
        var logger = NullLogger<WishlistFacade>.Instance;

        var user1 = new User { UserId = 1, FullName = "User One", StudentId = "STU-01", Email = "one@gmail.com", PasswordHash = "hash", RoleId = 1, IsActive = true };
        var user2 = new User { UserId = 2, FullName = "User Two", StudentId = "STU-02", Email = "two@gmail.com", PasswordHash = "hash", RoleId = 1, IsActive = true };
        context.Users.AddRange(user1, user2);

        var book1 = new Book { BookId = 1, Title = "C# in Depth", Author = "Jon Skeet", CategoryId = 1, Condition = BookCondition.Good, ExchangeType = ExchangeType.Sale, Price = 300, Description = "Desc", OwnerId = 1, Status = BookStatus.Available };
        var book2 = new Book { BookId = 2, Title = "CLR via C#", Author = "Jeffrey Richter", CategoryId = 1, Condition = BookCondition.Good, ExchangeType = ExchangeType.Sale, Price = 450, Description = "Desc", OwnerId = 2, Status = BookStatus.Available };
        context.Books.AddRange(book1, book2);
        context.SaveChanges();

        var facade = new WishlistFacade(wishlistRepo, bookRepo, logger);
        return (facade, context, user1, user2, book1, book2);
    }

    [Fact]
    public async Task ToggleWishlistAsync_OtherUserBook_AddsThenRemoves()
    {
        // Arrange
        var (facade, _, user1, _, _, book2) = SetupEnvironment(Guid.NewGuid().ToString());

        // Act 1: Toggle should add
        var addResult = await facade.ToggleWishlistAsync(user1.UserId, book2.BookId);
        Assert.True(addResult.Success);
        Assert.True(addResult.Data); // true = added
        Assert.True(await facade.IsInWishlistAsync(user1.UserId, book2.BookId));

        // Act 2: Toggle again should remove
        var removeResult = await facade.ToggleWishlistAsync(user1.UserId, book2.BookId);
        Assert.True(removeResult.Success);
        Assert.False(removeResult.Data); // false = removed
        Assert.False(await facade.IsInWishlistAsync(user1.UserId, book2.BookId));
    }

    [Fact]
    public async Task ToggleWishlistAsync_OwnBook_IsRejected()
    {
        // Arrange
        var (facade, _, user1, _, book1, _) = SetupEnvironment(Guid.NewGuid().ToString());

        // Act - User 1 tries to wishlist their own book
        var result = await facade.ToggleWishlistAsync(user1.UserId, book1.BookId);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("cannot add your own book", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RemoveFromWishlistAsync_Succeeds()
    {
        // Arrange
        var (facade, _, user1, _, _, book2) = SetupEnvironment(Guid.NewGuid().ToString());
        await facade.ToggleWishlistAsync(user1.UserId, book2.BookId);
        Assert.True(await facade.IsInWishlistAsync(user1.UserId, book2.BookId));

        // Act
        var removeResult = await facade.RemoveFromWishlistAsync(user1.UserId, book2.BookId);

        // Assert
        Assert.True(removeResult.Success);
        Assert.False(await facade.IsInWishlistAsync(user1.UserId, book2.BookId));
    }

    [Fact]
    public async Task GetUserWishlistAsync_ReturnsOnlyUserItems()
    {
        // Arrange
        var (facade, _, user1, user2, book1, book2) = SetupEnvironment(Guid.NewGuid().ToString());
        await facade.ToggleWishlistAsync(user1.UserId, book2.BookId);
        await facade.ToggleWishlistAsync(user2.UserId, book1.BookId);

        // Act
        var user1Wishlist = await facade.GetUserWishlistAsync(user1.UserId);

        // Assert
        Assert.Single(user1Wishlist.Items);
        Assert.Equal(book2.BookId, user1Wishlist.Items.First().BookId);
    }
}
