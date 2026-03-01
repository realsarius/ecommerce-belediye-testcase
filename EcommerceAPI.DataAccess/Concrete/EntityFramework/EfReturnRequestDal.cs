using EcommerceAPI.Core.DataAccess.EntityFramework;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.DataAccess.Concrete.EntityFramework;

public class EfReturnRequestDal : EfEntityRepositoryBase<ReturnRequest, AppDbContext>, IReturnRequestDal
{
    public EfReturnRequestDal(AppDbContext context) : base(context)
    {
    }

    public Task<ReturnRequest?> GetByIdWithDetailsAsync(int id)
    {
        return _context.ReturnRequests
            .Include(rr => rr.Order)
                .ThenInclude(order => order.Payment)
            .Include(rr => rr.Order)
                .ThenInclude(order => order.OrderItems)
                    .ThenInclude(item => item.Product)
            .Include(rr => rr.User)
            .Include(rr => rr.ReviewedByUser)
            .Include(rr => rr.RefundRequest)
            .FirstOrDefaultAsync(rr => rr.Id == id);
    }

    public async Task<IList<ReturnRequest>> GetUserRequestsAsync(int userId)
    {
        return await _context.ReturnRequests
            .Include(rr => rr.Order)
                .ThenInclude(order => order.Payment)
            .Include(rr => rr.User)
            .Include(rr => rr.ReviewedByUser)
            .Include(rr => rr.RefundRequest)
            .Where(rr => rr.UserId == userId)
            .OrderByDescending(rr => rr.CreatedAt)
            .ToListAsync();
    }

    public async Task<IList<ReturnRequest>> GetPendingRequestsAsync(int? sellerId = null)
    {
        var query = _context.ReturnRequests
            .Include(rr => rr.Order)
                .ThenInclude(order => order.Payment)
            .Include(rr => rr.Order)
                .ThenInclude(order => order.OrderItems)
                    .ThenInclude(item => item.Product)
            .Include(rr => rr.User)
            .Include(rr => rr.RefundRequest)
            .Where(rr => rr.Status == ReturnRequestStatus.Pending);

        if (sellerId.HasValue)
        {
            query = query.Where(rr => rr.Order.OrderItems.Any(item => item.Product.SellerId == sellerId.Value));
        }

        return await query
            .OrderBy(rr => rr.CreatedAt)
            .ToListAsync();
    }

    public Task<bool> HasActiveRequestForOrderAsync(int orderId)
    {
        return _context.ReturnRequests.AnyAsync(rr =>
            rr.OrderId == orderId &&
            (rr.Status == ReturnRequestStatus.Pending ||
             rr.Status == ReturnRequestStatus.Approved ||
             rr.Status == ReturnRequestStatus.RefundPending));
    }
}
