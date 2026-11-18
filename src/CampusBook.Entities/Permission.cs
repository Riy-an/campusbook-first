namespace CampusBook.Entities;

public class Permission
{
    public int PermissionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<PermissionGroupMap> GroupMaps { get; set; } = new List<PermissionGroupMap>();
    public virtual ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
}
