using EcommerceAPI.Core.DataAccess.EntityFramework;
using EcommerceAPI.Application.Abstractions.Persistence;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.DataAccess.Concrete.EntityFramework;

public class EfGiftCardTransactionDal : EfEntityRepositoryBase<GiftCardTransaction, AppDbContext>, IGiftCardTransactionDal
{
    public EfGiftCardTransactionDal(AppDbContext context) : base(context)
    {
    }

    public async Task<IList<GiftCardTransaction>> GetUserTransactionsAsync(int userId, int limit = 50)
    {
        return await _dbSet
            .Include(x => x.GiftCard)
            .Include(x => x.Order)
            .Where(x => x.GiftCard.AssignedUserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<GiftCardTransaction?> GetByOrderAndTypeAsync(int orderId, GiftCardTransactionType type)
    {
        return await _dbSet
            .Include(x => x.GiftCard)
            .FirstOrDefaultAsync(x => x.OrderId == orderId && x.Type == type);
    }
}
