using CampusBook.Entities;

namespace CampusBook.DataAccess.Interface;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByStudentIdAsync(string studentId);
    Task<User?> GetUserWithProfileAsync(int userId);
    Task<User?> GetByConfirmationTokenAsync(string token);
    Task<User?> GetByResetTokenAsync(string token);
    Task<bool> EmailExistsAsync(string email, int? excludeUserId = null);
    Task<bool> StudentIdExistsAsync(string studentId, int? excludeUserId = null);
    Task<Role?> GetRoleByNameAsync(string roleName);
}

