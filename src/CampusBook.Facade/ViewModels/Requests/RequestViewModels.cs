using System.ComponentModel.DataAnnotations;
using CampusBook.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CampusBook.Facade.ViewModels.Requests;

public class CreateRequestViewModel
{
    public int BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public string BookAuthor { get; set; } = string.Empty;
    public ExchangeType ExchangeType { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public string OwnerName { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Request message cannot exceed 1000 characters.")]
    [Display(Name = "Message to Book Owner (Optional)")]
    public string? RequestMessage { get; set; }

    [Display(Name = "Select a book from your available listings to offer in exchange")]
    public int? OfferedBookId { get; set; }

    public IEnumerable<SelectListItem> UserAvailableBooks { get; set; } = new List<SelectListItem>();
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
}

public class ReceivedRequestViewModel
{
    public int RequestId { get; set; }
    public int BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public int RequesterId { get; set; }
    public string RequesterName { get; set; } = string.Empty;
    public string RequesterDepartment { get; set; } = string.Empty;
    public string? OfferedBookTitle { get; set; }
    public string? RequestMessage { get; set; }
    public RequestStatus Status { get; set; }
    public bool OwnerConfirmed { get; set; }
    public bool RequesterConfirmed { get; set; }
    public DateTime CreatedAt { get; set; }
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

    // Offered Book (if exchange)
    public int? OfferedBookId { get; set; }
    public string? OfferedBookTitle { get; set; }
    public string? OfferedBookAuthor { get; set; }
    public string? OfferedBookImageUrl { get; set; }

    public string? RequestMessage { get; set; }
    public RequestStatus Status { get; set; }

    // Mutual handover confirmation flags
    public bool OwnerConfirmed { get; set; }
    public DateTime? OwnerConfirmedAt { get; set; }
    public bool RequesterConfirmed { get; set; }
    public DateTime? RequesterConfirmedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Context flags for active user
    public bool IsOwner { get; set; }
    public bool IsRequester { get; set; }
}

