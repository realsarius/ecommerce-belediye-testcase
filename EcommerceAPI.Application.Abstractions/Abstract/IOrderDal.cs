using EcommerceAPI.Core.DataAccess;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;

namespace EcommerceAPI.DataAccess.Abstract;

public interface IOrderDal : IEntityRepository<Order>
{
    Task<Order?> GetByIdWithDetailsAsync(int orderId);
    Task<IList<Order>> GetUserOrdersAsync(int userId);
    Task<Order?> GetByOrderNumberAsync(string orderNumber);
    Task<IList<Order>> GetExpiredPendingOrdersAsync(DateTime expiryTime);
    Task<IList<Order>> GetUserOrdersWithDetailsAsync(int userId);
    Task<IList<Order>> GetAllOrdersWithDetailsAsync();
    Task<IList<Order>> GetOrdersBySellerIdAsync(int sellerId);
    Task<IReadOnlyList<AdminDashboardOrderProjectionDto>> GetAdminDashboardOrderProjectionsAsync();
    Task<IReadOnlyList<AdminDashboardCategorySalesItemDto>> GetAdminDashboardCategorySalesAsync(int take = 6);
    Task<IReadOnlyList<int>> GetFrequentlyBoughtTogetherProductIdsAsync(int productId, int take = 8);
}
