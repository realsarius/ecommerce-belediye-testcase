using System.Collections.Generic;

namespace EcommerceAPI.Entities.DTOs;

public class WishlistDto
    : CursorPaginatedResponse<WishlistItemDto>
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? ActiveCollectionId { get; set; }
}

public class WishlistItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal ProductPrice { get; set; }
    public string ProductCurrency { get; set; } = string.Empty;
    public string? ProductImageUrl { get; set; }
    public int CollectionId { get; set; }
    public string CollectionName { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public string? UnavailableReason { get; set; }
    public DateTime AddedAt { get; set; }
    public decimal AddedAtPrice { get; set; }
    public decimal? PriceChange => ProductPrice - AddedAtPrice;
    public decimal? PriceChangePercentage => AddedAtPrice > 0 ? ((ProductPrice - AddedAtPrice) / AddedAtPrice) * 100 : null;
}
