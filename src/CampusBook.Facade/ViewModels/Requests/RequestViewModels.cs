using System.ComponentModel.DataAnnotations;
using CampusBook.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CampusBook.Facade.ViewModels.Requests;

public class CreateRequestViewModel
{
    // Requested Book Information (Read-only context)
    public int BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public string BookAuthor { get; set; } = string.Empty;
    public ExchangeType ExchangeType { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public string OwnerName { get; set; } = string.Empty;

    // Common optional message
    [StringLength(1000, ErrorMessage = "Request message cannot exceed 1000 characters.")]
    [Display(Name = "Message to Book Owner (Optional)")]
    public string? RequestMessage { get; set; }

    // Optional selection from requester's existing listings (convenience prefill)
    [Display(Name = "Choose from your active listings (Optional)")]
    public int? OfferedBookListingId { get; set; }
    public IEnumerable<SelectListItem> UserAvailableBooks { get; set; } = new List<SelectListItem>();

    // Structured Exchange Offer Fields (Mandatory when ExchangeType == Exchange)
    [StringLength(250, ErrorMessage = "Offered Book Title cannot exceed 250 characters.")]
    [Display(Name = "Offered Book Title")]
    public string? OfferTitle { get; set; }

    [StringLength(200, ErrorMessage = "Offered Book Author cannot exceed 200 characters.")]
    [Display(Name = "Author")]
    public string? OfferAuthor { get; set; }

    [StringLength(50, ErrorMessage = "ISBN cannot exceed 50 characters.")]
    [Display(Name = "ISBN (Optional)")]
    public string? OfferIsbn { get; set; }

    [StringLength(50, ErrorMessage = "Edition cannot exceed 50 characters.")]
    [Display(Name = "Edition (Optional)")]
    public string? OfferEdition { get; set; }

    [StringLength(20, ErrorMessage = "Publication Year cannot exceed 20 characters.")]
    [Display(Name = "Publication Year (Optional)")]
    public string? OfferPublicationYear { get; set; }

    [Display(Name = "Subject Category")]
    public int? OfferCategoryId { get; set; }

    [Display(Name = "Department (Optional)")]
    public int? OfferDepartmentId { get; set; }

    [StringLength(50, ErrorMessage = "Course Code cannot exceed 50 characters.")]
    [Display(Name = "Course Code (Optional)")]
    public string? OfferCourseCode { get; set; }

    [Display(Name = "Book Condition")]
    public BookCondition OfferCondition { get; set; } = BookCondition.Good;

    [StringLength(2500, ErrorMessage = "Description cannot exceed 2500 characters.")]
    [Display(Name = "Description of Offered Book")]
    public string? OfferDescription { get; set; }

    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters.")]
    [Display(Name = "Condition Notes / Highlighting / Damages (Optional)")]
    public string? OfferNotes { get; set; }

    [Display(Name = "Book Photo (Optional)")]
    public IFormFile? OfferImage { get; set; }

    // Dropdown helpers
    public IEnumerable<SelectListItem> Categories { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> Departments { get; set; } = new List<SelectListItem>();
}

public class ExchangeOfferDetailsViewModel
{
    public int ExchangeOfferId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string? Isbn { get; set; }
    public string? Edition { get; set; }
    public string? PublicationYear { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? DepartmentName { get; set; }
    public string? CourseCode { get; set; }
    public BookCondition Condition { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? ImageUrl { get; set; }
    public int? OfferedBookListingId { get; set; }
}

public class SentRequestViewModel
{
    public int RequestId { get; set; }
    public int BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public string BookAuthor { get; set; } = string.Empty;
    public ExchangeType ExchangeType { get; set; }
    public decimal Price { get; set; }
    public string? BookImageUrl { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public string? OfferedBookTitle { get; set; }
    public string? RequestMessage { get; set; }
    public RequestStatus Status { get; set; }
    public bool OwnerConfirmed { get; set; }
    public bool RequesterConfirmed { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? ConversationId { get; set; }
}

public class ReceivedRequestViewModel
{
    public int RequestId { get; set; }
    public int BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public int RequesterId { get; set; }
    public string RequesterName { get; set; } = string.Empty;
    public string RequesterDepartment { get; set; } = string.Empty;
    public ExchangeType ExchangeType { get; set; }
    public string? OfferedBookTitle { get; set; }
    public string? RequestMessage { get; set; }
    public RequestStatus Status { get; set; }
    public bool OwnerConfirmed { get; set; }
    public bool RequesterConfirmed { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? ConversationId { get; set; }
}

public class RequestDetailsViewModel
{
    public int RequestId { get; set; }

    // Requested Book details
    public int BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public string BookAuthor { get; set; } = string.Empty;
    public ExchangeType ExchangeType { get; set; }
    public decimal Price { get; set; }
    public string? BookImageUrl { get; set; }

    // Owner details
    public int OwnerId { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public string OwnerDepartment { get; set; } = string.Empty;
    public string? OwnerEmail { get; set; }

    // Requester details
    public int RequesterId { get; set; }
    public string RequesterName { get; set; } = string.Empty;
    public string RequesterDepartment { get; set; } = string.Empty;
    public string RequesterBatch { get; set; } = string.Empty;
    public string? RequesterEmail { get; set; }

    // Structured Offered Book details (for Exchange)
    public ExchangeOfferDetailsViewModel? ExchangeOffer { get; set; }

    public string? RequestMessage { get; set; }
    public RequestStatus Status { get; set; }

    // Mutual handover confirmation flags
    public bool OwnerConfirmed { get; set; }
    public DateTime? OwnerConfirmedAt { get; set; }
    public bool RequesterConfirmed { get; set; }
    public DateTime? RequesterConfirmedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Messaging connection
    public int? ConversationId { get; set; }

    // Context flags for active user
    public bool IsOwner { get; set; }
    public bool IsRequester { get; set; }
}
