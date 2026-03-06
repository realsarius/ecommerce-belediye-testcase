using EcommerceAPI.Core.DataAccess;
using EcommerceAPI.Entities.Concrete;

namespace EcommerceAPI.DataAccess.Abstract;

public interface IReferralTransactionDal : IEntityRepository<ReferralTransaction>
{
    Task<IList<ReferralTransaction>> GetUserTransactionsAsync(int userId, int limit = 50);
    Task<IList<ReferralTransaction>> GetGrantedTransactionsByOrderAsync(int orderId);
    Task<int> GetTotalRewardPointsAsync(int beneficiaryUserId);
}
