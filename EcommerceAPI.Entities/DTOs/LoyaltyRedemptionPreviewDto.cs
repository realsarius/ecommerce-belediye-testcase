using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Entities.DTOs;

public class LoyaltyRedemptionPreviewDto : IDto
{
    public int RequestedPoints { get; set; }
    public int AppliedPoints { get; set; }
    public int AvailablePoints { get; set; }
    public decimal DiscountAmount { get; set; }
}
