using CampusBook.Entities;

namespace CampusBook.Facade.ViewModels.Wishlist;

public class WishlistItemViewModel
{
    public int WishlistItemId { get; set; }
    public int BookId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string? Isbn { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public BookCondition Condition { get; set; }
    public ExchangeType ExchangeType { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public BookStatus Status { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public DateTime AddedAt { get; set; }
}

public class WishlistPageViewModel
{
    public IEnumerable<WishlistItemViewModel> Items { get; set; } = new List<WishlistItemViewModel>();
    public int TotalCount => Items.Count();
}
