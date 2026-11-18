using System.Security.Claims;
using CampusBook.Facade.Interface;
using Microsoft.AspNetCore.Authorization;

namespace CampusBook.Web.UI.Authorization;

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IPermissionService _permissionService;
    private readonly ILogger<PermissionAuthorizationHandler> _logger;

    public PermissionAuthorizationHandler(
        IPermissionService permissionService,
        ILogger<PermissionAuthorizationHandler> logger)
    {
        _permissionService = permissionService;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // 1. Fail closed if user is unauthenticated
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            return;
        }

        // 2. Extract user ID claim
        var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId) || userId <= 0)
        {
            _logger.LogWarning("Permission check failed: Invalid or missing NameIdentifier claim.");
            return;
        }

        // 3. Evaluate database-backed permissions (with in-memory sliding cache)
        var hasPermission = await _permissionService.HasPermissionAsync(userId, requirement.Permission);

        if (hasPermission)
        {
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning("Access denied: User {UserId} lacks permission '{Permission}'", userId, requirement.Permission);
            // Fail closed: do not call context.Succeed, leaving the requirement unfulfilled
        }
    }
}
