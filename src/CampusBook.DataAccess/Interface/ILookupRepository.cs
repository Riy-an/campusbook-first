using CampusBook.Entities;

namespace CampusBook.DataAccess.Interface;

public interface ILookupRepository
{
    Task<IEnumerable<Department>> GetAllDepartmentsAsync();
    Task<IEnumerable<BookCategory>> GetAllCategoriesAsync();
    Task<Department?> GetDepartmentByIdAsync(int id);
    Task<BookCategory?> GetCategoryByIdAsync(int id);
}

