using EcommerceAPI.Core.DataAccess.EntityFramework;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;

namespace EcommerceAPI.DataAccess.Concrete.EntityFramework;

public class EfWishlistItemDal : EfEntityRepositoryBase<WishlistItem, AppDbContext>, IWishlistItemDal
{
    public EfWishlistItemDal(AppDbContext context) : base(context)
    {
    }
}
