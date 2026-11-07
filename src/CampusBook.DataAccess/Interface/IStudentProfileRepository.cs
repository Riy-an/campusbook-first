using CampusBook.Entities;

namespace CampusBook.DataAccess.Interface;

public interface IStudentProfileRepository : IRepository<StudentProfile>
{
    Task<StudentProfile?> GetByUserIdAsync(int userId);
    Task<StudentProfile?> GetProfileWithUserAsync(int userId);
}

