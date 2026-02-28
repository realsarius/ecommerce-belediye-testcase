namespace EcommerceAPI.Entities.Concrete;

public class WishlistCollection : BaseEntity
{
    public int WishlistId { get; set; }
    public Wishlist Wishlist { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }

    public ICollection<WishlistItem> Items { get; set; } = new List<WishlistItem>();
}
