using EcommerceAPI.Core.DataAccess.EntityFramework;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.DataAccess.Concrete.EntityFramework;

public class EfSellerProfileDal : EfEntityRepositoryBase<SellerProfile, AppDbContext>, ISellerProfileDal
{
    public EfSellerProfileDal(AppDbContext context) : base(context) { }

    public async Task<SellerProfile?> GetByUserIdWithDetailsAsync(int userId)
    {
        return await _dbSet.Include(sp => sp.User).FirstOrDefaultAsync(sp => sp.UserId == userId);
    }

    public async Task<SellerProfile?> GetByIdWithDetailsAsync(int id)
    {
        return await _dbSet.Include(sp => sp.User).FirstOrDefaultAsync(sp => sp.Id == id);
    }

    public async Task<List<SellerProfile>> GetAdminListWithDetailsAsync()
    {
        return await BuildAdminSellerQuery()
            .OrderBy(sp => sp.BrandName)
            .ToListAsync();
    }

    public async Task<SellerProfile?> GetAdminDetailWithDetailsAsync(int id)
    {
        return await BuildAdminSellerQuery().FirstOrDefaultAsync(sp => sp.Id == id);
    }

    private IQueryable<SellerProfile> BuildAdminSellerQuery()
    {
        return _dbSet
            .Include(sp => sp.User)
            .Include(sp => sp.Products)
                .ThenInclude(product => product.Category)
            .Include(sp => sp.Products)
                .ThenInclude(product => product.Inventory)
            .Include(sp => sp.Products)
                .ThenInclude(product => product.Reviews)
            .Include(sp => sp.Products)
                .ThenInclude(product => product.OrderItems)
                    .ThenInclude(orderItem => orderItem.Order);
    }
}
