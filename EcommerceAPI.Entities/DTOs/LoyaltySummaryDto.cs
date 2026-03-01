using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Entities.DTOs;

public class LoyaltySummaryDto : IDto
{
    public int AvailablePoints { get; set; }
    public decimal AvailableDiscountAmount { get; set; }
    public int TotalEarnedPoints { get; set; }
    public int TotalRedeemedPoints { get; set; }
    public int PointsPerLira { get; set; }
    public List<LoyaltyTransactionDto> RecentTransactions { get; set; } = new();
}
