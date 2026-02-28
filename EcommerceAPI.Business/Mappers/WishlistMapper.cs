using System.Linq;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;

namespace EcommerceAPI.Business.Mappers;

public class WishlistMapper : IWishlistMapper
{
    public WishlistDto ToWishlistDto(Wishlist wishlist)
    {
        if (wishlist == null) return null!;

        return new WishlistDto
        {
            Id = wishlist.Id,
            UserId = wishlist.UserId,
            Items = wishlist.Items != null 
                ? wishlist.Items.Select(ToWishlistItemDto).ToList() 
                : new List<WishlistItemDto>()
        };
    }

    public WishlistItemDto ToWishlistItemDto(WishlistItem item)
    {
        if (item == null) return null!;

        return new WishlistItemDto
        {
            Id = item.Id,
            ProductId = item.ProductId,
            ProductName = item.Product?.Name ?? string.Empty,
            ProductPrice = item.Product?.Price ?? 0,
            ProductCurrency = item.Product?.Currency ?? "TRY",
            AddedAt = item.AddedAt,
            AddedAtPrice = item.AddedAtPrice
        };
    }
}
