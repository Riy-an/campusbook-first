using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace CampusBook.Web.UI.Authorization;

public class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallbackPolicyProvider;

    public const string PolicyPrefix = "Permission:";

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
    }

    public async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (string.IsNullOrWhiteSpace(policyName))
        {
            return await _fallbackPolicyProvider.GetPolicyAsync(policyName);
        }

        // Check if policy represents a permission (e.g. "Permission:Books.Create" or direct permission name "Books.Create")
        string permissionName;
        if (policyName.StartsWith(PolicyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            permissionName = policyName[PolicyPrefix.Length..];
        }
        else if (policyName.Contains('.'))
        {
            // Direct permission notation: e.g. "Books.Create", "Requests.Create"
            permissionName = policyName;
        }
        else
        {
            // Standard fallback policy
            return await _fallbackPolicyProvider.GetPolicyAsync(policyName);
        }

        var policy = new AuthorizationPolicyBuilder();
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new PermissionRequirement(permissionName));
        return policy.Build();
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() =>
        _fallbackPolicyProvider.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() =>
        _fallbackPolicyProvider.GetFallbackPolicyAsync();
}
