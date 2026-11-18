using CampusBook.Entities;
using CampusBook.Facade.Common;

namespace CampusBook.Facade.Interface;

public interface IPermissionService
{
    Task<bool> HasPermissionAsync(int userId, string permissionName);
    Task<IReadOnlySet<string>> GetUserPermissionsAsync(int userId);
    void InvalidateUserPermissionsCache(int userId);
    void InvalidateAllPermissionsCache();
    Task<ServiceResult> AssignGroupToUserAsync(int userId, string groupName);
    Task<ServiceResult> RemoveGroupFromUserAsync(int userId, string groupName);
    Task<ServiceResult> GrantDirectPermissionAsync(int userId, string permissionName);
    Task<ServiceResult> RevokeDirectPermissionAsync(int userId, string permissionName);
    Task<IEnumerable<PermissionGroup>> GetGroupsAsync();
    Task<IEnumerable<Permission>> GetAllPermissionsAsync();
}
