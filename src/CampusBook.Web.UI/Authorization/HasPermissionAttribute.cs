using Microsoft.AspNetCore.Authorization;

namespace CampusBook.Web.UI.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class HasPermissionAttribute : AuthorizeAttribute
{
    public string Permission { get; }

    public HasPermissionAttribute(string permission)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);
        Permission = permission;
        Policy = permission;
    }
}
