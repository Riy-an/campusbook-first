using Microsoft.AspNetCore.Authorization;

namespace CampusBook.Web.UI.Authorization;

public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public PermissionRequirement(string permission)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);
        Permission = permission;
    }
}
