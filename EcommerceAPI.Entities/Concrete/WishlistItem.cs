namespace EcommerceAPI.Entities.Concrete;

public class WishlistItem : BaseEntity
{
    public int WishlistId { get; set; }
    public Wishlist Wishlist { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    /// <summary>
    /// Ürünün favoriye eklendiği andaki fiyatı
    /// </summary>
    public decimal AddedAtPrice { get; set; }

    /// <summary>
    /// Ürünün favoriye eklendiği tarih
    /// </summary>
    public DateTime AddedAt { get; set; }
}
