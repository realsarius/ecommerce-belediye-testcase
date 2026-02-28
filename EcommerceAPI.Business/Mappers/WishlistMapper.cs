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

        var isAvailable = item.Product?.IsActive == true;
        var fallbackPrice = item.AddedAtPrice > 0 ? item.AddedAtPrice : item.Product?.Price ?? 0;
        var fallbackAddedAt = item.AddedAt != default ? item.AddedAt : item.CreatedAt;

        return new WishlistItemDto
        {
            Id = item.Id,
            ProductId = item.ProductId,
            ProductName = item.Product?.Name ?? "Ürün artık mevcut değil",
            ProductPrice = item.Product?.Price ?? fallbackPrice,
            ProductCurrency = item.Product?.Currency ?? "TRY",
            CollectionId = item.CollectionId,
            CollectionName = item.Collection?.Name ?? "Favorilerim",
            IsAvailable = isAvailable,
            UnavailableReason = isAvailable ? null : "Bu ürün artık mevcut değil.",
            AddedAt = fallbackAddedAt,
            AddedAtPrice = fallbackPrice
        };
    }
}
