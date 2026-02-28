using EcommerceAPI.Core.DataAccess.EntityFramework;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;

namespace EcommerceAPI.DataAccess.Concrete.EntityFramework;

public class EfWishlistDal : EfEntityRepositoryBase<Wishlist, AppDbContext>, IWishlistDal
{
    public EfWishlistDal(AppDbContext context) : base(context)
    {
    }
}
