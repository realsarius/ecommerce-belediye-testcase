using EcommerceAPI.Core.DataAccess.EntityFramework;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;

namespace EcommerceAPI.DataAccess.Concrete.EntityFramework;

public class EfRefreshTokenDal : EfEntityRepositoryBase<RefreshToken, AppDbContext>, IRefreshTokenDal
{
    public EfRefreshTokenDal(AppDbContext context) : base(context) { }
}
