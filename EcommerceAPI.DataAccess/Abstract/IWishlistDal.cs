using EcommerceAPI.Core.DataAccess;
using EcommerceAPI.Entities.Concrete;

namespace EcommerceAPI.DataAccess.Abstract;

public interface IWishlistDal : IEntityRepository<Wishlist>
{
    Task<Wishlist> GetOrCreateByUserIdAsync(int userId);
}
