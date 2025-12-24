using EcommerceAPI.Core.DataAccess.EntityFramework;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;

namespace EcommerceAPI.DataAccess.Concrete.EntityFramework;

public class EfSellerProfileDal : EfEntityRepositoryBase<SellerProfile, AppDbContext>, ISellerProfileDal
{
    public EfSellerProfileDal(AppDbContext context) : base(context) { }
}
