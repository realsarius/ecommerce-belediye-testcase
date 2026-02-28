using System.Collections.Generic;

namespace EcommerceAPI.Entities.DTOs;

public class WishlistDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public List<WishlistItemDto> Items { get; set; } = new();
}

public class WishlistItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal ProductPrice { get; set; }
    public string ProductCurrency { get; set; } = string.Empty;
    public string? ProductImageUrl { get; set; }
    public bool IsAvailable { get; set; }
    public string? UnavailableReason { get; set; }
    public DateTime AddedAt { get; set; }
    public decimal AddedAtPrice { get; set; }
    public decimal? PriceChange => ProductPrice - AddedAtPrice;
    public decimal? PriceChangePercentage => AddedAtPrice > 0 ? ((ProductPrice - AddedAtPrice) / AddedAtPrice) * 100 : null;
}
