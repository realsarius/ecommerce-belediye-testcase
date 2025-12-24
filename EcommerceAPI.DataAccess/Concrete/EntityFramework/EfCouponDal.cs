using EcommerceAPI.Core.DataAccess.EntityFramework;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;

namespace EcommerceAPI.DataAccess.Concrete.EntityFramework;

public class EfCouponDal : EfEntityRepositoryBase<Coupon, AppDbContext>, ICouponDal
{
    public EfCouponDal(AppDbContext context) : base(context) { }

    public async Task<Coupon?> GetByCodeAsync(string code)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Code.ToUpper() == code.ToUpper());
    }
}
