using CampusBook.DataAccess;
using CampusBook.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CampusBook.Web.UI.Infrastructure;

public static class AdminSeeder
{
    public static async Task EnsureAdminUserAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("AdminSeeder");

        try
        {
            // Check if any Admin user exists
            var adminExists = await dbContext.Users.AnyAsync(u => u.RoleId == 2);
            if (!adminExists)
            {
                var adminUser = new User
                {
                    FullName = "System Administrator",
                    StudentId = "ADMIN-001",
                    Email = "admin@campusbook.edu",
                    RoleId = 2, // Admin Role
                    IsEmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    StudentProfile = new StudentProfile
                    {
                        Department = "Administration",
                        Batch = "STAFF",
                        UniversityEmail = "admin@campusbook.edu",
                        UpdatedAt = DateTime.UtcNow
                    }
                };

                adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, "Admin@123456");

                // Assign to Admin Permission Group (GroupId = 2)
                adminUser.UserPermissions.Add(new UserPermission
                {
                    PermissionGroupId = 2,
                    IsGranted = true,
                    CreatedAt = DateTime.UtcNow
                });

                await dbContext.Users.AddAsync(adminUser);
                await dbContext.SaveChangesAsync();
                logger.LogInformation("Initial administrator account created: admin@campusbook.edu");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while seeding default administrator account");
        }
    }
}
