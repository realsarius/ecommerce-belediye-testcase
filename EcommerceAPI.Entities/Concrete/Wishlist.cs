namespace EcommerceAPI.Entities.Concrete;

public class Wishlist : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public bool IsPublic { get; set; }
    public Guid? ShareToken { get; set; }
    
    public ICollection<WishlistItem> Items { get; set; } = new List<WishlistItem>();
}
