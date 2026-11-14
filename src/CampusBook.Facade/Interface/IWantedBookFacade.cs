using CampusBook.Entities;
using CampusBook.Facade.Common;
using CampusBook.Facade.ViewModels.WantedBooks;

namespace CampusBook.Facade.Interface;

public interface IWantedBookFacade
{
    Task<WantedBookCreateViewModel> GetCreateViewModelAsync();
    Task<ServiceResult<int>> CreateWantedBookAsync(int userId, WantedBookCreateViewModel model);
    Task<WantedBookEditViewModel?> GetEditViewModelAsync(int wantedBookId, int userId);
    Task<ServiceResult> UpdateWantedBookAsync(int userId, WantedBookEditViewModel model);
    Task<ServiceResult> CloseWantedBookAsync(int userId, int wantedBookId);
    Task<IEnumerable<WantedBookItemViewModel>> GetMyWantedBooksAsync(int userId);
    Task<WantedBookFilterViewModel> GetActiveWantedBooksAsync(string? searchTerm, int? categoryId, int? departmentId);
    Task<WantedBookDetailsViewModel?> GetWantedBookDetailsAsync(int wantedBookId, int currentUserId);
    Task CheckAndNotifyMatchesForNewBookAsync(Book book);
}
