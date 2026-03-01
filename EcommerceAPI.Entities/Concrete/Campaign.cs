using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Entities.Concrete;

public class Campaign : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? BadgeText { get; set; }
    public CampaignType Type { get; set; } = CampaignType.FlashSale;
    public CampaignStatus Status { get; set; } = CampaignStatus.Draft;
    public bool IsEnabled { get; set; } = true;
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public ICollection<CampaignProduct> CampaignProducts { get; set; } = new List<CampaignProduct>();
}
