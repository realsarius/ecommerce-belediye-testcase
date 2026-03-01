namespace EcommerceAPI.Entities.Concrete;

public class CampaignProduct : BaseEntity
{
    public int CampaignId { get; set; }
    public Campaign Campaign { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public decimal CampaignPrice { get; set; }
    public decimal OriginalPriceSnapshot { get; set; }
    public bool IsFeatured { get; set; }
}
