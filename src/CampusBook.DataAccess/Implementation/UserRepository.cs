using CampusBook.DataAccess.Interface;
using CampusBook.Entities;
using Microsoft.EntityFrameworkCore;

namespace CampusBook.DataAccess.Implementation;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        var normalizedEmail = email.Trim().ToLower();
        return await _dbContext.Users
            .Include(u => u.Role)
            .Include(u => u.StudentProfile)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);
    }

    public async Task<User?> GetByStudentIdAsync(string studentId)
    {
        var normalizedStudentId = studentId.Trim();
        return await _dbContext.Users
            .Include(u => u.Role)
            .Include(u => u.StudentProfile)
            .FirstOrDefaultAsync(u => u.StudentId == normalizedStudentId);
    }

    public async Task<User?> GetUserWithProfileAsync(int userId)
    {
        return await _dbContext.Users
            .Include(u => u.Role)
            .Include(u => u.StudentProfile)
            .FirstOrDefaultAsync(u => u.UserId == userId);
    }

    public async Task<User?> GetByConfirmationTokenAsync(string token)
    {
        return await _dbContext.Users
            .Include(u => u.Role)
            .Include(u => u.StudentProfile)
            .FirstOrDefaultAsync(u => u.EmailConfirmationToken == token);
    }

    public async Task<User?> GetByResetTokenAsync(string token)
    {
        return await _dbContext.Users
            .Include(u => u.Role)
            .Include(u => u.StudentProfile)
            .FirstOrDefaultAsync(u => u.PasswordResetToken == token);
    }

    public async Task<bool> EmailExistsAsync(string email, int? excludeUserId = null)
    {
        var normalizedEmail = email.Trim().ToLower();
        return await _dbContext.Users
            .AnyAsync(u => u.Email.ToLower() == normalizedEmail && (!excludeUserId.HasValue || u.UserId != excludeUserId.Value));
    }

    public async Task<bool> StudentIdExistsAsync(string studentId, int? excludeUserId = null)
    {
        var normalizedStudentId = studentId.Trim();
        return await _dbContext.Users
            .AnyAsync(u => u.StudentId == normalizedStudentId && (!excludeUserId.HasValue || u.UserId != excludeUserId.Value));
    }

    public async Task<Role?> GetRoleByNameAsync(string roleName)
    {
        return await _dbContext.Roles
            .FirstOrDefaultAsync(r => r.RoleName == roleName);
    }
}

