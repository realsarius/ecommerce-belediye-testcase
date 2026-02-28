using EcommerceAPI.Core.DataAccess;
using EcommerceAPI.Entities.Concrete;

namespace EcommerceAPI.DataAccess.Abstract;

public interface IWishlistItemDal : IEntityRepository<WishlistItem>
{
    Task<bool> AddIfNotExistsAsync(WishlistItem item);
    Task<int> DeleteByWishlistAndProductAsync(int wishlistId, int productId);
    Task<IList<WishlistItem>> GetPagedByWishlistIdAsync(int wishlistId, DateTime? cursorAddedAt, int? cursorItemId, int take);
}
