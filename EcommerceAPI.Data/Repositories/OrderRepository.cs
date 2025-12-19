using EcommerceAPI.Core.Entities;
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
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.Payment)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
    }

    public async Task<List<Order>> GetUserOrdersAsync(int userId)
    {
        return await _dbSet
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Order?> GetByIdWithDetailsAsync(int orderId)
    {
        return await _dbSet
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(o => o.Id == orderId);
    }
}
