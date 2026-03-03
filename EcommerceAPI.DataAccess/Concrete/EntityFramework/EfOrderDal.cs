using EcommerceAPI.Core.DataAccess.EntityFramework;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
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
            .Include(o => o.InvoiceInfo)
            .Include(o => o.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId);
    }

    public async Task<IList<Order>> GetUserOrdersAsync(int userId)
    {
        return await _dbSet
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.Payment)
            .Include(o => o.InvoiceInfo)
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
            .Include(o => o.InvoiceInfo)
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
            .Include(o => o.InvoiceInfo)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IList<Order>> GetAllOrdersWithDetailsAsync()
    {
        return await _dbSet
            .Include(o => o.User)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.Payment)
            .Include(o => o.InvoiceInfo)
            .OrderByDescending(o => o.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IList<Order>> GetOrdersBySellerIdAsync(int sellerId)
    {
        return await _dbSet
            .Include(o => o.User)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.Payment)
            .Include(o => o.InvoiceInfo)
            .Where(o => o.OrderItems.Any(oi => oi.Product.SellerId == sellerId))
            .OrderByDescending(o => o.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IReadOnlyList<AdminDashboardOrderProjectionDto>> GetAdminDashboardOrderProjectionsAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .Select(order => new AdminDashboardOrderProjectionDto
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                CustomerName = order.User == null
                    ? string.Empty
                    : ((order.User.FirstName ?? string.Empty) + " " + (order.User.LastName ?? string.Empty)).Trim(),
                TotalAmount = order.TotalAmount,
                Currency = order.Currency,
                Status = order.Status,
                CreatedAt = order.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<IReadOnlyList<AdminDashboardCategorySalesItemDto>> GetAdminDashboardCategorySalesAsync(int take = 6)
    {
        return await _context.OrderItems
            .AsNoTracking()
            .Where(item =>
                item.Product != null &&
                item.Product.Category != null &&
                item.Order.Status != OrderStatus.Cancelled &&
                item.Order.Status != OrderStatus.PendingPayment)
            .GroupBy(item => item.Product.Category.Name)
            .Select(group => new AdminDashboardCategorySalesItemDto
            {
                CategoryName = group.Key,
                SalesCount = group.Sum(item => item.Quantity)
            })
            .OrderByDescending(item => item.SalesCount)
            .ThenBy(item => item.CategoryName)
            .Take(Math.Clamp(take, 1, 20))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<int>> GetFrequentlyBoughtTogetherProductIdsAsync(int productId, int take = 8)
    {
        var orderIds = await _context.OrderItems
            .Where(item => item.ProductId == productId)
            .Select(item => item.OrderId)
            .Distinct()
            .ToListAsync();

        if (orderIds.Count == 0)
        {
            return [];
        }

        return await _context.OrderItems
            .Where(item => orderIds.Contains(item.OrderId) && item.ProductId != productId)
            .Join(
                _context.Orders.Where(order =>
                    order.Payment != null &&
                    order.Payment.Status == PaymentStatus.Success &&
                    order.Status != OrderStatus.Cancelled),
                item => item.OrderId,
                order => order.Id,
                (item, _) => item.ProductId)
            .GroupBy(relatedProductId => relatedProductId)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key)
            .Select(group => group.Key)
            .Take(take)
            .ToListAsync();
    }
}
