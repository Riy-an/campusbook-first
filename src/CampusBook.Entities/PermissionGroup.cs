namespace CampusBook.Entities;

public class PermissionGroup
{
    public int PermissionGroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystem { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<PermissionGroupMap> GroupMaps { get; set; } = new List<PermissionGroupMap>();
    public virtual ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
}
