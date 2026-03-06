using EcommerceAPI.Core.DataAccess;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.DataAccess.Abstract;

public interface IGiftCardTransactionDal : IEntityRepository<GiftCardTransaction>
{
    Task<IList<GiftCardTransaction>> GetUserTransactionsAsync(int userId, int limit = 50);
    Task<GiftCardTransaction?> GetByOrderAndTypeAsync(int orderId, GiftCardTransactionType type);
}
