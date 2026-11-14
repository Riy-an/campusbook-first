using CampusBook.Facade.Common;
using CampusBook.Facade.ViewModels.Wishlist;

namespace CampusBook.Facade.Interface;

public interface IWishlistFacade
{
    Task<ServiceResult<bool>> ToggleWishlistAsync(int userId, int bookId);
    Task<ServiceResult> RemoveFromWishlistAsync(int userId, int bookId);
    Task<WishlistPageViewModel> GetUserWishlistAsync(int userId);
    Task<bool> IsInWishlistAsync(int userId, int bookId);
}
