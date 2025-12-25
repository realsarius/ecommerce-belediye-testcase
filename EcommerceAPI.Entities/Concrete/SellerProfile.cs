namespace EcommerceAPI.Entities.Concrete;

public class SellerProfile : BaseEntity
{
    public int UserId { get; set; }
    public string BrandName { get; set; } = string.Empty;
    public string? BrandDescription { get; set; }
    public string? LogoUrl { get; set; }
    public bool IsVerified { get; set; } = false;
    
    public User User { get; set; } = null!;
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
