using EcommerceAPI.Core.DataAccess.EntityFramework;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.DataAccess.Concrete.EntityFramework;

public class EfLoyaltyTransactionDal : EfEntityRepositoryBase<LoyaltyTransaction, AppDbContext>, ILoyaltyTransactionDal
{
    public EfLoyaltyTransactionDal(AppDbContext context) : base(context)
    {
    }

    public async Task<int> GetAvailablePointsAsync(int userId, DateTime utcNow)
    {
        return await _dbSet
            .Where(x => x.UserId == userId && (x.ExpiresAt == null || x.ExpiresAt > utcNow))
            .SumAsync(x => x.Points);
    }

    public async Task<int> GetTotalPointsByTypeAsync(int userId, LoyaltyTransactionType type)
    {
        return await _dbSet
            .Where(x => x.UserId == userId && x.Type == type)
            .SumAsync(x => x.Points);
    }

    public async Task<IList<LoyaltyTransaction>> GetUserTransactionsAsync(int userId, int limit = 50)
    {
        return await _dbSet
            .Include(x => x.Order)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<LoyaltyTransaction?> GetByOrderAndTypeAsync(int orderId, LoyaltyTransactionType type)
    {
        return await _dbSet
            .FirstOrDefaultAsync(x => x.OrderId == orderId && x.Type == type);
    }
}
