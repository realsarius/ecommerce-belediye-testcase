using EcommerceAPI.Core.DataAccess;
using EcommerceAPI.Entities.Concrete;

namespace EcommerceAPI.DataAccess.Abstract;

public interface IWishlistItemDal : IEntityRepository<WishlistItem>
{
    Task<bool> AddIfNotExistsAsync(WishlistItem item);
    Task<int> DeleteByWishlistAndProductAsync(int wishlistId, int productId);
}
