namespace CampusBook.Entities;

public class PermissionGroupMap
{
    public int PermissionGroupId { get; set; }
    public virtual PermissionGroup PermissionGroup { get; set; } = null!;

    public int PermissionId { get; set; }
    public virtual Permission Permission { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
