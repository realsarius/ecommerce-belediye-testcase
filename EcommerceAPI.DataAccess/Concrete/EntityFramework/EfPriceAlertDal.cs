using EcommerceAPI.Core.DataAccess.EntityFramework;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.DataAccess.Concrete.EntityFramework;

public class EfPriceAlertDal : EfEntityRepositoryBase<PriceAlert, AppDbContext>, IPriceAlertDal
{
    public EfPriceAlertDal(AppDbContext context) : base(context)
    {
    }

    public Task<PriceAlert?> GetByUserAndProductAsync(int userId, int productId)
    {
        return _context.PriceAlerts
            .Include(pa => pa.Product)
            .FirstOrDefaultAsync(pa => pa.UserId == userId && pa.ProductId == productId);
    }

    public async Task<IList<PriceAlert>> GetUserAlertsWithProductsAsync(int userId)
    {
        return await _context.PriceAlerts
            .Include(pa => pa.Product)
            .Where(pa => pa.UserId == userId && pa.IsActive)
            .OrderByDescending(pa => pa.CreatedAt)
            .ToListAsync();
    }

    public async Task<IList<PriceAlert>> GetActiveAlertsWithProductsAsync()
    {
        return await _context.PriceAlerts
            .Include(pa => pa.Product)
            .Where(pa => pa.IsActive)
            .ToListAsync();
    }
}
