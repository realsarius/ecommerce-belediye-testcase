using EcommerceAPI.Core.DataAccess;
using EcommerceAPI.Entities.Concrete;

namespace EcommerceAPI.Application.Abstractions.Persistence;

public interface IWishlistDal : IEntityRepository<Wishlist>
{
    Task<Wishlist> GetOrCreateByUserIdAsync(int userId);
    Task<Wishlist?> GetByShareTokenAsync(Guid shareToken);
}
