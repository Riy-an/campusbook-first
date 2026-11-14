using CampusBook.DataAccess.Interface;
using CampusBook.Entities;
using CampusBook.Facade.Common;
using CampusBook.Facade.Interface;
using CampusBook.Facade.ViewModels.Wishlist;
using Microsoft.Extensions.Logging;

namespace CampusBook.Facade.Implementation;

public class WishlistFacade : IWishlistFacade
{
    private readonly IWishlistRepository _wishlistRepository;
    private readonly IBookRepository _bookRepository;
    private readonly ILogger<WishlistFacade> _logger;

    public WishlistFacade(
        IWishlistRepository wishlistRepository,
        IBookRepository bookRepository,
        ILogger<WishlistFacade> logger)
    {
        _wishlistRepository = wishlistRepository;
        _bookRepository = bookRepository;
        _logger = logger;
    }

    public async Task<ServiceResult<bool>> ToggleWishlistAsync(int userId, int bookId)
    {
        var book = await _bookRepository.GetByIdAsync(bookId);
        if (book == null)
        {
            return ServiceResult<bool>.Fail("Book not found.");
        }

        // Prevent adding own book to wishlist
        if (book.OwnerId == userId)
        {
            return ServiceResult<bool>.Fail("You cannot add your own book to your wishlist.");
        }

        var existing = await _wishlistRepository.GetByUserAndBookAsync(userId, bookId);
        if (existing != null)
        {
            // Remove from wishlist
            await _wishlistRepository.DeleteAsync(existing);
            _logger.LogInformation("User {UserId} removed Book {BookId} from wishlist", userId, bookId);
            return ServiceResult<bool>.Ok(false); // false = not in wishlist anymore
        }
        else
        {
            // Add to wishlist
            var item = new WishlistItem
            {
                UserId = userId,
                BookId = bookId,
                CreatedAt = DateTime.UtcNow
            };
            await _wishlistRepository.AddAsync(item);
            _logger.LogInformation("User {UserId} added Book {BookId} to wishlist", userId, bookId);
            return ServiceResult<bool>.Ok(true); // true = now in wishlist
        }
    }

    public async Task<ServiceResult> RemoveFromWishlistAsync(int userId, int bookId)
    {
        var existing = await _wishlistRepository.GetByUserAndBookAsync(userId, bookId);
        if (existing != null)
        {
            await _wishlistRepository.DeleteAsync(existing);
            return ServiceResult.Ok();
        }

        return ServiceResult.Fail("Book is not in your wishlist.");
    }

    public async Task<WishlistPageViewModel> GetUserWishlistAsync(int userId)
    {
        var items = await _wishlistRepository.GetUserWishlistAsync(userId);
        return new WishlistPageViewModel
        {
            Items = items.Select(w => new WishlistItemViewModel
            {
                WishlistItemId = w.WishlistItemId,
                BookId = w.BookId,
                Title = w.Book.Title,
                Author = w.Book.Author,
                Isbn = w.Book.Isbn,
                CategoryName = w.Book.Category?.Name ?? "General",
                Condition = w.Book.Condition,
                ExchangeType = w.Book.ExchangeType,
                Price = w.Book.Price,
                ImageUrl = w.Book.ImageUrl,
                Status = w.Book.Status,
                OwnerName = w.Book.Owner?.FullName ?? "Student",
                AddedAt = w.CreatedAt
            })
        };
    }

    public async Task<bool> IsInWishlistAsync(int userId, int bookId)
    {
        return await _wishlistRepository.ExistsAsync(userId, bookId);
    }
}
