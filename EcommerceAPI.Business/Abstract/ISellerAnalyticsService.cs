using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Entities.DTOs;

namespace EcommerceAPI.Business.Abstract;

public interface ISellerAnalyticsService
{
    Task<IDataResult<SellerAnalyticsSummaryDto>> GetSummaryAsync(int sellerId);
    Task<IDataResult<List<SellerAnalyticsTrendPointDto>>> GetTrendAsync(int sellerId, int days = 30);
}
