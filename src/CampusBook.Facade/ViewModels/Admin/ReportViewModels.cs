using System.ComponentModel.DataAnnotations;
using CampusBook.Entities;

namespace CampusBook.Facade.ViewModels.Admin;

public class ReportListViewModel
{
    public IEnumerable<Report> Reports { get; set; } = new List<Report>();
    public int TotalCount { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 15;
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    public ReportStatus? Status { get; set; }
    public ReportTargetType? TargetType { get; set; }
}

public class ReportResolutionViewModel
{
    public int ReportId { get; set; }
    public ReportStatus NewStatus { get; set; }

    [Required(ErrorMessage = "Resolution notes are required")]
    [StringLength(2000, ErrorMessage = "Resolution notes cannot exceed 2000 characters")]
    public string ResolutionNotes { get; set; } = string.Empty;
}

public class CreateReportViewModel
{
    public ReportTargetType TargetType { get; set; }
    public int TargetId { get; set; }
    public string TargetTitle { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please state the reason for your report")]
    [StringLength(100)]
    public string Reason { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please provide details describing the problem")]
    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
