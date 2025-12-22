using EcommerceAPI.Core.Entities;
using EcommerceAPI.Core.Enums;
using EcommerceAPI.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.Data.Repositories;

public class OrderRepository : GenericRepository<Order>, IOrderRepository
{
    public OrderRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Order?> GetByOrderNumberAsync(string orderNumber)
    {
        return await _dbSet
            .Include(o => o.User)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
    }

    public async Task<List<Order>> GetUserOrdersAsync(int userId)
    {
        return await _dbSet
            .Where(o => o.UserId == userId)
            .Include(o => o.User)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .OrderByDescending(o => o.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Order?> GetByIdWithDetailsAsync(int orderId)
    {
        return await _dbSet
            .Include(o => o.User)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(o => o.Id == orderId);
    }

    public async Task<List<Order>> GetExpiredPendingOrdersAsync(int timeoutMinutes)
    {
        var cutoffTime = DateTime.UtcNow.AddMinutes(-timeoutMinutes);

        return await _dbSet
            .Include(o => o.OrderItems)
            .Where(o => o.Status == OrderStatus.PendingPayment && 
                        o.CreatedAt < cutoffTime)
            .ToListAsync();
    }
    public async Task<List<Order>> GetAllWithDetailsAsync()
    {
        return await _dbSet
            .Include(o => o.User)
            .Include(o => o.OrderItems)
            .Include(o => o.Payment)
            .OrderByDescending(o => o.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }
}
