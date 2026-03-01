using EcommerceAPI.Core.DataAccess;
using EcommerceAPI.Entities.Concrete;

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
    Task<IReadOnlyList<int>> GetFrequentlyBoughtTogetherProductIdsAsync(int productId, int take = 8);
}
