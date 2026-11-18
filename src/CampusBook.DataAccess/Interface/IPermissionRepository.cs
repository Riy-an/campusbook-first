using CampusBook.Entities;

namespace CampusBook.DataAccess.Interface;

public interface IPermissionRepository : IRepository<Permission>
{
    Task<bool> HasPermissionAsync(int userId, string permissionName);
    Task<IReadOnlySet<string>> GetUserEffectivePermissionsAsync(int userId);
    Task<PermissionGroup?> GetGroupByNameAsync(string groupName);
    Task<PermissionGroup?> GetGroupByIdAsync(int groupId);
    Task<IEnumerable<PermissionGroup>> GetAllGroupsAsync();
    Task<IEnumerable<Permission>> GetAllPermissionsAsync();
    Task<IEnumerable<PermissionGroupMap>> GetGroupMapsAsync(int groupId);
    Task AssignUserToGroupAsync(int userId, int groupId);
    Task RemoveUserFromGroupAsync(int userId, int groupId);
    Task SetUserDirectPermissionAsync(int userId, int permissionId, bool isGranted);
    Task RemoveUserDirectPermissionAsync(int userId, int permissionId);
}
