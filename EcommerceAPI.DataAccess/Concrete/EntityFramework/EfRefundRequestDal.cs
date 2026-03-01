using EcommerceAPI.Core.DataAccess.EntityFramework;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.DataAccess.Concrete.EntityFramework;

public class EfRefundRequestDal : EfEntityRepositoryBase<RefundRequest, AppDbContext>, IRefundRequestDal
{
    public EfRefundRequestDal(AppDbContext context) : base(context)
    {
    }

    public Task<RefundRequest?> GetByIdWithDetailsAsync(int id)
    {
        return _context.RefundRequests
            .Include(rr => rr.Order)
                .ThenInclude(order => order.Payment)
            .Include(rr => rr.ReturnRequest)
                .ThenInclude(returnRequest => returnRequest.User)
            .FirstOrDefaultAsync(rr => rr.Id == id);
    }

    public Task<RefundRequest?> GetByReturnRequestIdAsync(int returnRequestId)
    {
        return _context.RefundRequests
            .Include(rr => rr.Order)
                .ThenInclude(order => order.Payment)
            .Include(rr => rr.ReturnRequest)
                .ThenInclude(returnRequest => returnRequest.User)
            .FirstOrDefaultAsync(rr => rr.ReturnRequestId == returnRequestId);
    }
}
