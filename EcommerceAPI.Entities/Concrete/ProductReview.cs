namespace EcommerceAPI.Entities.Concrete;

public class ProductReview : BaseEntity
{
    public int ProductId { get; set; }
    public int UserId { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;

    // Navigation
    public Product Product { get; set; } = null!;
    public User User { get; set; } = null!;
}
