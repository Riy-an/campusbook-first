using CampusBook.DataAccess.Interface;
using CampusBook.Entities;
using CampusBook.Facade.Common;
using CampusBook.Facade.Interface;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace CampusBook.Facade.Implementation;

public class PermissionService : IPermissionService
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<PermissionService> _logger;

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(2);
    private const string CacheKeyPrefix = "UserPermissions_";

    public PermissionService(
        IPermissionRepository permissionRepository,
        IMemoryCache memoryCache,
        ILogger<PermissionService> logger)
    {
        _permissionRepository = permissionRepository;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public async Task<bool> HasPermissionAsync(int userId, string permissionName)
    {
        if (userId <= 0 || string.IsNullOrWhiteSpace(permissionName))
        {
            return false;
        }

        var permissions = await GetUserPermissionsAsync(userId);
        return permissions.Contains(permissionName);
    }

    public async Task<IReadOnlySet<string>> GetUserPermissionsAsync(int userId)
    {
        if (userId <= 0)
        {
            return new HashSet<string>();
        }

        var cacheKey = $"{CacheKeyPrefix}{userId}";
        if (_memoryCache.TryGetValue(cacheKey, out IReadOnlySet<string>? cached) && cached != null)
        {
            return cached;
        }

        var permissions = await _permissionRepository.GetUserEffectivePermissionsAsync(userId);

        var cacheOptions = new MemoryCacheEntryOptions
        {
            SlidingExpiration = CacheDuration,
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };

        _memoryCache.Set(cacheKey, permissions, cacheOptions);
        return permissions;
    }

    public void InvalidateUserPermissionsCache(int userId)
    {
        if (userId > 0)
        {
            var cacheKey = $"{CacheKeyPrefix}{userId}";
            _memoryCache.Remove(cacheKey);
            _logger.LogInformation("Invalidated permission cache for User {UserId}", userId);
        }
    }

    public void InvalidateAllPermissionsCache()
    {
        // For memory cache without prefix eviction, resetting specific keys is preferred,
        // but this provides a programmatic hook when groups/definitions change.
        _logger.LogInformation("Global permission cache invalidation signaled");
    }

    public async Task<ServiceResult> AssignGroupToUserAsync(int userId, string groupName)
    {
        if (userId <= 0) return ServiceResult.Fail("Invalid user identifier.");

        var group = await _permissionRepository.GetGroupByNameAsync(groupName);
        if (group == null)
        {
            return ServiceResult.Fail($"Permission group '{groupName}' not found.");
        }

        await _permissionRepository.AssignUserToGroupAsync(userId, group.PermissionGroupId);
        InvalidateUserPermissionsCache(userId);

        _logger.LogInformation("Assigned group {GroupName} to User {UserId}", groupName, userId);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> RemoveGroupFromUserAsync(int userId, string groupName)
    {
        if (userId <= 0) return ServiceResult.Fail("Invalid user identifier.");

        var group = await _permissionRepository.GetGroupByNameAsync(groupName);
        if (group == null)
        {
            return ServiceResult.Fail($"Permission group '{groupName}' not found.");
        }

        await _permissionRepository.RemoveUserFromGroupAsync(userId, group.PermissionGroupId);
        InvalidateUserPermissionsCache(userId);

        _logger.LogInformation("Removed group {GroupName} from User {UserId}", groupName, userId);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> GrantDirectPermissionAsync(int userId, string permissionName)
    {
        if (userId <= 0) return ServiceResult.Fail("Invalid user identifier.");

        var permissions = await _permissionRepository.GetAllPermissionsAsync();
        var permission = permissions.FirstOrDefault(p => string.Equals(p.Name, permissionName, StringComparison.OrdinalIgnoreCase));
        if (permission == null)
        {
            return ServiceResult.Fail($"Permission '{permissionName}' not found.");
        }

        await _permissionRepository.SetUserDirectPermissionAsync(userId, permission.PermissionId, true);
        InvalidateUserPermissionsCache(userId);

        _logger.LogInformation("Granted direct permission {PermissionName} to User {UserId}", permissionName, userId);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> RevokeDirectPermissionAsync(int userId, string permissionName)
    {
        if (userId <= 0) return ServiceResult.Fail("Invalid user identifier.");

        var permissions = await _permissionRepository.GetAllPermissionsAsync();
        var permission = permissions.FirstOrDefault(p => string.Equals(p.Name, permissionName, StringComparison.OrdinalIgnoreCase));
        if (permission == null)
        {
            return ServiceResult.Fail($"Permission '{permissionName}' not found.");
        }

        // Setting IsGranted = false creates an explicit deny that overrides any group grants
        await _permissionRepository.SetUserDirectPermissionAsync(userId, permission.PermissionId, false);
        InvalidateUserPermissionsCache(userId);

        _logger.LogInformation("Explicitly revoked permission {PermissionName} from User {UserId}", permissionName, userId);
        return ServiceResult.Ok();
    }

    public async Task<IEnumerable<PermissionGroup>> GetGroupsAsync()
    {
        return await _permissionRepository.GetAllGroupsAsync();
    }

    public async Task<IEnumerable<Permission>> GetAllPermissionsAsync()
    {
        return await _permissionRepository.GetAllPermissionsAsync();
    }
}
