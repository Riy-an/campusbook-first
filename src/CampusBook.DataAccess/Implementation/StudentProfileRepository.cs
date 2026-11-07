using CampusBook.DataAccess.Interface;
using CampusBook.Entities;
using Microsoft.EntityFrameworkCore;

namespace CampusBook.DataAccess.Implementation;

public class StudentProfileRepository : Repository<StudentProfile>, IStudentProfileRepository
{
    public StudentProfileRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<StudentProfile?> GetByUserIdAsync(int userId)
    {
        return await _dbContext.StudentProfiles
            .FirstOrDefaultAsync(sp => sp.UserId == userId);
    }

    public async Task<StudentProfile?> GetProfileWithUserAsync(int userId)
    {
        return await _dbContext.StudentProfiles
            .Include(sp => sp.User)
            .FirstOrDefaultAsync(sp => sp.UserId == userId);
    }
}

