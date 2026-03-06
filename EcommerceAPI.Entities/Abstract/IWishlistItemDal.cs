using EcommerceAPI.Core.DataAccess;
using EcommerceAPI.Entities.Concrete;

namespace EcommerceAPI.DataAccess.Abstract;

public interface IWishlistItemDal : IEntityRepository<WishlistItem>
{
    Task<bool> AddIfNotExistsAsync(WishlistItem item);
    Task<int> DeleteByWishlistAndProductAsync(int wishlistId, int productId);
    Task<int> MoveToCollectionAsync(int wishlistId, int productId, int collectionId);
    Task<IList<WishlistItem>> GetByWishlistIdWithDetailsAsync(int wishlistId, int? collectionId = null);
    Task<IList<WishlistItem>> GetPagedByWishlistIdAsync(int wishlistId, DateTime? cursorAddedAt, int? cursorItemId, int take, int? collectionId = null);
}
