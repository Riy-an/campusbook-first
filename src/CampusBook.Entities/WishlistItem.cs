namespace CampusBook.Entities;

public class WishlistItem
{
    public int WishlistItemId { get; set; }

    public int UserId { get; set; }
    public virtual User User { get; set; } = null!;

    public int BookId { get; set; }
    public virtual Book Book { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
