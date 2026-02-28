using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;

namespace EcommerceAPI.Business.Mappers;

public interface IWishlistMapper
{
    WishlistDto ToWishlistDto(Wishlist wishlist);
    WishlistItemDto ToWishlistItemDto(WishlistItem item);
}
