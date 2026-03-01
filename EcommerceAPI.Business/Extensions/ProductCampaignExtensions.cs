using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Business.Extensions;

public static class ProductCampaignExtensions
{
    public static CampaignProduct? GetActiveCampaignProduct(this Product product, DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;

        return product.CampaignProducts
            .Where(x =>
                x.Campaign != null &&
                x.Campaign.IsEnabled &&
                x.Campaign.Status == CampaignStatus.Active &&
                x.Campaign.StartsAt <= now &&
                x.Campaign.EndsAt > now)
            .OrderBy(x => x.CampaignPrice)
            .ThenByDescending(x => x.IsFeatured)
            .ThenBy(x => x.Campaign!.EndsAt)
            .FirstOrDefault();
    }

    public static decimal GetEffectivePrice(this Product product, DateTime? utcNow = null)
    {
        return product.GetActiveCampaignProduct(utcNow)?.CampaignPrice ?? product.Price;
    }
}
