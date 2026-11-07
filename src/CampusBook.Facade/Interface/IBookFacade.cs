using CampusBook.Facade.Common;
using CampusBook.Facade.ViewModels.Books;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CampusBook.Facade.Interface;

public interface IBookFacade
{
    Task<BookFilterViewModel> GetCatalogueAsync(BookFilterViewModel filter);
    Task<BookDetailsViewModel?> GetBookDetailsAsync(int bookId, int? currentUserId);
    Task<ServiceResult<int>> CreateBookAsync(int ownerId, BookCreateViewModel model);
    Task<BookEditViewModel?> GetBookForEditAsync(int bookId, int ownerId);
    Task<ServiceResult> UpdateBookAsync(int ownerId, BookEditViewModel model);
    Task<ServiceResult> DeactivateBookAsync(int bookId, int ownerId);
    Task<IEnumerable<BookItemViewModel>> GetUserBooksAsync(int userId);
    Task<IEnumerable<SelectListItem>> GetUserAvailableBooksDropdownAsync(int userId, int? selectedBookId = null);
    Task<IEnumerable<SelectListItem>> GetCategoriesDropdownAsync(int? selectedCategoryId = null);
    Task<IEnumerable<SelectListItem>> GetDepartmentsDropdownAsync(int? selectedDepartmentId = null);
}

