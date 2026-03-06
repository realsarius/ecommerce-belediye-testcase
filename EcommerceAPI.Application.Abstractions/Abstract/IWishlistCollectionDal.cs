using EcommerceAPI.Core.DataAccess;
using EcommerceAPI.Entities.Concrete;

namespace EcommerceAPI.DataAccess.Abstract;

public interface IWishlistCollectionDal : IEntityRepository<WishlistCollection>
{
    Task<WishlistCollection> GetOrCreateDefaultCollectionAsync(int wishlistId);
    Task<IList<WishlistCollection>> GetByWishlistIdAsync(int wishlistId);
    Task<bool> ExistsByNameAsync(int wishlistId, string name);
}
