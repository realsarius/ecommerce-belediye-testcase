using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Entities.DTOs;

namespace EcommerceAPI.Application.Abstractions.ServiceContracts;

public interface IAdminDashboardService
{
    Task<IDataResult<AdminDashboardKpiDto>> GetKpiAsync();
    Task<IDataResult<List<AdminDashboardRevenueTrendPointDto>>> GetRevenueTrendAsync(string period = "daily");
    Task<IDataResult<List<AdminDashboardCategorySalesItemDto>>> GetCategorySalesAsync();
    Task<IDataResult<List<AdminDashboardUserRegistrationPointDto>>> GetUserRegistrationsAsync(int days = 30);
    Task<IDataResult<List<AdminDashboardOrderStatusDistributionItemDto>>> GetOrderStatusDistributionAsync();
    Task<IDataResult<List<AdminDashboardLowStockItemDto>>> GetLowStockAsync(int threshold = 5);
    Task<IDataResult<List<AdminDashboardRecentOrderDto>>> GetRecentOrdersAsync(int limit = 5);
}
