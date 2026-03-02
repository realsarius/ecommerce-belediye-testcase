using EcommerceAPI.Core.DataAccess.EntityFramework;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.DataAccess.Concrete.EntityFramework;

public class EfReferralTransactionDal : EfEntityRepositoryBase<ReferralTransaction, AppDbContext>, IReferralTransactionDal
{
    public EfReferralTransactionDal(AppDbContext context) : base(context)
    {
    }

    public async Task<IList<ReferralTransaction>> GetUserTransactionsAsync(int userId, int limit = 50)
    {
        return await _dbSet
            .Include(x => x.Order)
            .Include(x => x.ReferralCode)
            .Include(x => x.ReferrerUser)
            .Include(x => x.ReferredUser)
            .Where(x =>
                x.ReferrerUserId == userId ||
                x.ReferredUserId == userId ||
                x.BeneficiaryUserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IList<ReferralTransaction>> GetGrantedTransactionsByOrderAsync(int orderId)
    {
        return await _dbSet
            .Include(x => x.Order)
            .Include(x => x.ReferralCode)
            .Where(x =>
                x.OrderId == orderId &&
                (x.Type == ReferralTransactionType.ReferrerRewardGranted ||
                 x.Type == ReferralTransactionType.ReferredRewardGranted))
            .ToListAsync();
    }

    public async Task<int> GetTotalRewardPointsAsync(int beneficiaryUserId)
    {
        return await _dbSet
            .Where(x =>
                x.BeneficiaryUserId == beneficiaryUserId &&
                x.Type != ReferralTransactionType.Signup)
            .SumAsync(x => x.Points);
    }
}
