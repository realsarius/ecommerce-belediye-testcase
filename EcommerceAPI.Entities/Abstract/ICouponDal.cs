using EcommerceAPI.Core.DataAccess;
using EcommerceAPI.Entities.Concrete;

namespace EcommerceAPI.DataAccess.Abstract;

public interface ICouponDal : IEntityRepository<Coupon>
{
    Task<Coupon?> GetByCodeAsync(string code);
}
