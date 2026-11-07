using CampusBook.DataAccess.Interface;
using CampusBook.Entities;
using Microsoft.EntityFrameworkCore;

namespace CampusBook.DataAccess.Implementation;

public class LookupRepository : ILookupRepository
{
    private readonly ApplicationDbContext _dbContext;

    public LookupRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<Department>> GetAllDepartmentsAsync()
    {
        return await _dbContext.Departments
            .OrderBy(d => d.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<BookCategory>> GetAllCategoriesAsync()
    {
        return await _dbContext.BookCategories
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Department?> GetDepartmentByIdAsync(int id)
    {
        return await _dbContext.Departments.FindAsync(id);
    }

    public async Task<BookCategory?> GetCategoryByIdAsync(int id)
    {
        return await _dbContext.BookCategories.FindAsync(id);
    }
}

