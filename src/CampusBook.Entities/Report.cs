namespace CampusBook.Entities;

public class Report
{
    public int ReportId { get; set; }

    public int ReporterId { get; set; }
    public virtual User Reporter { get; set; } = null!;

    public ReportTargetType TargetType { get; set; }
    public int TargetId { get; set; }

    public string Reason { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public ReportStatus Status { get; set; } = ReportStatus.Pending;

    public int? AssignedAdminId { get; set; }
    public virtual User? AssignedAdmin { get; set; }

    public string? ResolutionNotes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
}
