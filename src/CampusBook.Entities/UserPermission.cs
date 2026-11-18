namespace CampusBook.Entities;

public class UserPermission
{
    public int UserPermissionId { get; set; }

    public int UserId { get; set; }
    public virtual User User { get; set; } = null!;

    public int? PermissionGroupId { get; set; }
    public virtual PermissionGroup? PermissionGroup { get; set; }

    public int? PermissionId { get; set; }
    public virtual Permission? Permission { get; set; }

    public bool IsGranted { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
