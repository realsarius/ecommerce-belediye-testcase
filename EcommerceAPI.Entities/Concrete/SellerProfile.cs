namespace EcommerceAPI.Entities.Concrete;

public class SellerProfile : BaseEntity
{
    public int UserId { get; set; }
    public string BrandName { get; set; } = string.Empty;
    public string? BrandDescription { get; set; }
    public string? LogoUrl { get; set; }
    public string? BannerImageUrl { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? InstagramUrl { get; set; }
    public string? FacebookUrl { get; set; }
    public string? XUrl { get; set; }
    public bool IsVerified { get; set; } = false;
    public decimal? CommissionRateOverride { get; set; }
    public string? ApplicationReviewNote { get; set; }
    public DateTime? ApplicationReviewedAt { get; set; }
    
    public User User { get; set; } = null!;
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
