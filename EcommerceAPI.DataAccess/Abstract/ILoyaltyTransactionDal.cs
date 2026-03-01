using EcommerceAPI.Core.DataAccess;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.DataAccess.Abstract;

public interface ILoyaltyTransactionDal : IEntityRepository<LoyaltyTransaction>
{
    Task<int> GetAvailablePointsAsync(int userId, DateTime utcNow);
    Task<int> GetTotalPointsByTypeAsync(int userId, LoyaltyTransactionType type);
    Task<IList<LoyaltyTransaction>> GetUserTransactionsAsync(int userId, int limit = 50);
    Task<LoyaltyTransaction?> GetByOrderAndTypeAsync(int orderId, LoyaltyTransactionType type);
}
