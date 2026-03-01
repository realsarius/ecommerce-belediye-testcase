using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Entities.DTOs;

namespace EcommerceAPI.Business.Abstract;

public interface ILoyaltyService
{
    Task<IDataResult<LoyaltySummaryDto>> GetSummaryAsync(int userId, int recentLimit = 10);
    Task<IDataResult<List<LoyaltyTransactionDto>>> GetHistoryAsync(int userId, int limit = 50);
    Task<IDataResult<LoyaltyRedemptionPreviewDto>> CalculateRedemptionAsync(int userId, int requestedPoints, decimal orderTotal);
    Task<IResult> RedeemPointsForOrderAsync(int userId, int orderId, int points, decimal discountAmount, string description);
    Task<IResult> RestoreRedeemedPointsAsync(int userId, int orderId, string description);
    Task<IResult> AwardPointsForOrderAsync(int userId, int orderId, decimal paidAmount);
    Task<IResult> ReverseEarnedPointsAsync(int userId, int orderId, string description);
}
