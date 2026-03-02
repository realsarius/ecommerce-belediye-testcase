using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Entities.DTOs;

public class ReferralSummaryDto : IDto
{
    public string ReferralCode { get; set; } = string.Empty;
    public int TotalReferrals { get; set; }
    public int SuccessfulReferrals { get; set; }
    public int PendingReferrals { get; set; }
    public int TotalRewardPoints { get; set; }
    public int ReferrerRewardPoints { get; set; }
    public int ReferredRewardPoints { get; set; }
    public string? ReferredByCode { get; set; }
    public List<ReferralTransactionDto> RecentTransactions { get; set; } = new();
}

public class ReferralTransactionDto : IDto
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public int Points { get; set; }
    public int? OrderId { get; set; }
    public string? OrderNumber { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? RelatedUserName { get; set; }
    public DateTime CreatedAt { get; set; }
}
