using EcommerceAPI.Core.DataAccess.EntityFramework;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.Enums;
using Microsoft.EntityFrameworkCore;

using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;

namespace EcommerceAPI.DataAccess.Concrete.EntityFramework;

public class EfOrderDal : EfEntityRepositoryBase<Order, AppDbContext>, IOrderDal
{
    public EfOrderDal(AppDbContext context) : base(context) { }

    public async Task<Order?> GetByIdWithDetailsAsync(int orderId)
    {
        return await _dbSet
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.Payment)
            .Include(o => o.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId);
    }

    public async Task<IList<Order>> GetUserOrdersAsync(int userId)
    {
        return await _dbSet
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Order?> GetByOrderNumberAsync(string orderNumber)
    {
        return await _dbSet
            .Include(o => o.OrderItems)
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
    }

    public async Task<IList<Order>> GetExpiredPendingOrdersAsync(DateTime expiryTime)
    {
        return await _dbSet
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                    .ThenInclude(p => p.Inventory)
            .Where(o => o.Status == OrderStatus.PendingPayment && o.CreatedAt < expiryTime)
            .ToListAsync();
    }

    public async Task<IList<Order>> GetUserOrdersWithDetailsAsync(int userId)
    {
        return await _dbSet
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.Payment)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }
}
