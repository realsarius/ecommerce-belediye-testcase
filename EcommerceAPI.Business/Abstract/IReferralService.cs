using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Entities.DTOs;

namespace EcommerceAPI.Business.Abstract;

public interface IReferralService
{
    Task<IDataResult<ReferralSummaryDto>> GetSummaryAsync(int userId, int recentLimit = 10);
    Task<IDataResult<List<ReferralTransactionDto>>> GetHistoryAsync(int userId, int limit = 50);
    Task<IResult> ValidateReferralCodeAsync(string? referralCode);
    Task<IResult> SetupNewUserAsync(int userId, string? referralCode = null);
    Task<IResult> AwardFirstPurchaseRewardsAsync(int orderId);
    Task<IResult> ReverseRewardsForOrderAsync(int orderId, string description);
}
