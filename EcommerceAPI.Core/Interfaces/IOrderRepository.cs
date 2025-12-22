using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Core.Interfaces;

public interface IOrderRepository : IRepository<Order>
{
    Task<Order?> GetByOrderNumberAsync(string orderNumber);
    Task<List<Order>> GetUserOrdersAsync(int userId);
    Task<Order?> GetByIdWithDetailsAsync(int orderId);
    Task<List<Order>> GetExpiredPendingOrdersAsync(int timeoutMinutes);
    Task<List<Order>> GetAllWithDetailsAsync();
}
