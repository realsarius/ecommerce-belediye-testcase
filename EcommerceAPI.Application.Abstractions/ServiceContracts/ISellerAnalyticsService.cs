using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Entities.DTOs;

namespace EcommerceAPI.Application.Abstractions.ServiceContracts;

public interface ISellerAnalyticsService
{
    Task<IDataResult<SellerAnalyticsSummaryDto>> GetSummaryAsync(int sellerId);
    Task<IDataResult<List<SellerAnalyticsTrendPointDto>>> GetTrendAsync(int sellerId, int days = 30);
    Task<IDataResult<SellerFinanceSummaryDto>> GetFinanceSummaryAsync(int sellerId, int days = 30, DateOnly? from = null, DateOnly? to = null);
    Task<IDataResult<SellerDashboardKpiDto>> GetDashboardKpiAsync(int sellerId, int days = 30);
    Task<IDataResult<List<SellerDashboardRevenueTrendPointDto>>> GetDashboardRevenueTrendAsync(int sellerId, string period = "daily");
    Task<IDataResult<List<SellerDashboardOrderStatusDistributionItemDto>>> GetDashboardOrderStatusDistributionAsync(int sellerId);
    Task<IDataResult<List<SellerDashboardProductPerformanceItemDto>>> GetDashboardProductPerformanceAsync(int sellerId, int take = 5);
    Task<IDataResult<List<SellerDashboardRecentOrderDto>>> GetDashboardRecentOrdersAsync(int sellerId, int take = 5);
}
