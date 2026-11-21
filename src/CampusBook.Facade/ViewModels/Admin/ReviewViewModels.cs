using System.ComponentModel.DataAnnotations;
using CampusBook.Entities;

namespace CampusBook.Facade.ViewModels.Admin;

public class ReviewModerationListViewModel
{
    public IEnumerable<ExchangeReview> Reviews { get; set; } = new List<ExchangeReview>();
    public int TotalCount { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 15;
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    public bool? IsHidden { get; set; }
}

public class CreateReviewViewModel
{
    public int RequestId { get; set; }
    public int ReviewedUserId { get; set; }
    public string ReviewedUserName { get; set; } = string.Empty;
    public string BookTitle { get; set; } = string.Empty;

    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5 stars")]
    public int Rating { get; set; } = 5;

    [Required(ErrorMessage = "Please write a comment")]
    [StringLength(2000, ErrorMessage = "Comment cannot exceed 2000 characters")]
    public string Comment { get; set; } = string.Empty;
}
