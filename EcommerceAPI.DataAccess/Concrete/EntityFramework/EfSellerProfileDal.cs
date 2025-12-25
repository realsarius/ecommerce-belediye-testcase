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
}
