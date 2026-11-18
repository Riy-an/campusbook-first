using CampusBook.DataAccess.Interface;
using CampusBook.Entities;
using Microsoft.EntityFrameworkCore;

namespace CampusBook.DataAccess.Implementation;

public class PermissionRepository : Repository<Permission>, IPermissionRepository
{
    public PermissionRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<bool> HasPermissionAsync(int userId, string permissionName)
    {
        if (userId <= 0 || string.IsNullOrWhiteSpace(permissionName))
        {
            return false;
        }

        var permissions = await GetUserEffectivePermissionsAsync(userId);
        return permissions.Contains(permissionName);
    }

    public async Task<IReadOnlySet<string>> GetUserEffectivePermissionsAsync(int userId)
    {
        if (userId <= 0)
        {
            return new HashSet<string>();
        }

        // Check user existence and active status
        var user = await _dbContext.Users
            .AsNoTracking()
            .Select(u => new { u.UserId, u.IsActive, u.RoleId })
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null || !user.IsActive)
        {
            return new HashSet<string>();
        }

        // 1. Gather all group IDs assigned to user (explicit assignments + baseline role)
        var explicitGroupIds = await _dbContext.UserPermissions
            .AsNoTracking()
            .Where(up => up.UserId == userId && up.PermissionGroupId.HasValue)
            .Select(up => up.PermissionGroupId!.Value)
            .ToListAsync();

        var groupIds = new HashSet<int>(explicitGroupIds);
        if (user.RoleId > 0)
        {
            groupIds.Add(user.RoleId);
        }

        // 2. Fetch all permission names from mapped groups
        var groupPermissions = await _dbContext.PermissionGroupMaps
            .AsNoTracking()
            .Where(pgm => groupIds.Contains(pgm.PermissionGroupId))
            .Select(pgm => pgm.Permission.Name)
            .ToListAsync();

        var effectivePermissions = new HashSet<string>(groupPermissions, StringComparer.OrdinalIgnoreCase);

        // 3. Apply direct user permission overrides (explicit grants and revocations)
        var directOverrides = await _dbContext.UserPermissions
            .AsNoTracking()
            .Where(up => up.UserId == userId && up.PermissionId.HasValue)
            .Select(up => new { PermissionName = up.Permission!.Name, up.IsGranted })
            .ToListAsync();

        foreach (var overrideItem in directOverrides)
        {
            if (overrideItem.IsGranted)
            {
                effectivePermissions.Add(overrideItem.PermissionName);
            }
            else
            {
                // Explicit deny takes precedence over group grants
                effectivePermissions.Remove(overrideItem.PermissionName);
            }
        }

        return effectivePermissions;
    }

    public async Task<PermissionGroup?> GetGroupByNameAsync(string groupName)
    {
        return await _dbContext.PermissionGroups
            .FirstOrDefaultAsync(g => g.GroupName.ToLower() == groupName.ToLower());
    }

    public async Task<PermissionGroup?> GetGroupByIdAsync(int groupId)
    {
        return await _dbContext.PermissionGroups
            .Include(g => g.GroupMaps)
                .ThenInclude(gm => gm.Permission)
            .FirstOrDefaultAsync(g => g.PermissionGroupId == groupId);
    }

    public async Task<IEnumerable<PermissionGroup>> GetAllGroupsAsync()
    {
        return await _dbContext.PermissionGroups
            .OrderBy(g => g.PermissionGroupId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Permission>> GetAllPermissionsAsync()
    {
        return await _dbContext.Permissions
            .OrderBy(p => p.Module)
            .ThenBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<PermissionGroupMap>> GetGroupMapsAsync(int groupId)
    {
        return await _dbContext.PermissionGroupMaps
            .Include(gm => gm.Permission)
            .Where(gm => gm.PermissionGroupId == groupId)
            .ToListAsync();
    }

    public async Task AssignUserToGroupAsync(int userId, int groupId)
    {
        var existing = await _dbContext.UserPermissions
            .FirstOrDefaultAsync(up => up.UserId == userId && up.PermissionGroupId == groupId);

        if (existing == null)
        {
            await _dbContext.UserPermissions.AddAsync(new UserPermission
            {
                UserId = userId,
                PermissionGroupId = groupId,
                IsGranted = true,
                CreatedAt = DateTime.UtcNow
            });
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task RemoveUserFromGroupAsync(int userId, int groupId)
    {
        var existing = await _dbContext.UserPermissions
            .FirstOrDefaultAsync(up => up.UserId == userId && up.PermissionGroupId == groupId);

        if (existing != null)
        {
            _dbContext.UserPermissions.Remove(existing);
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task SetUserDirectPermissionAsync(int userId, int permissionId, bool isGranted)
    {
        var existing = await _dbContext.UserPermissions
            .FirstOrDefaultAsync(up => up.UserId == userId && up.PermissionId == permissionId);

        if (existing != null)
        {
            existing.IsGranted = isGranted;
            _dbContext.UserPermissions.Update(existing);
        }
        else
        {
            await _dbContext.UserPermissions.AddAsync(new UserPermission
            {
                UserId = userId,
                PermissionId = permissionId,
                IsGranted = isGranted,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _dbContext.SaveChangesAsync();
    }

    public async Task RemoveUserDirectPermissionAsync(int userId, int permissionId)
    {
        var existing = await _dbContext.UserPermissions
            .FirstOrDefaultAsync(up => up.UserId == userId && up.PermissionId == permissionId);

        if (existing != null)
        {
            _dbContext.UserPermissions.Remove(existing);
            await _dbContext.SaveChangesAsync();
        }
    }
}
