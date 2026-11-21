namespace CampusBook.Entities;

public class AuditLog
{
    public int AuditLogId { get; set; }

    public int? AdminUserId { get; set; }
    public virtual User? AdminUser { get; set; }

    public string Action { get; set; } = string.Empty;
    public string TargetEntityType { get; set; } = string.Empty;
    public string? TargetId { get; set; }
    public string Details { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public bool IsSuccess { get; set; } = true;
}
