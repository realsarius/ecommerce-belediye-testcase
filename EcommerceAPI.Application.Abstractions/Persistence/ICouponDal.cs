using EcommerceAPI.Core.DataAccess;
using EcommerceAPI.Entities.Concrete;

namespace EcommerceAPI.Application.Abstractions.Persistence;

public interface ICouponDal : IEntityRepository<Coupon>
{
    Task<Coupon?> GetByCodeAsync(string code);
}
