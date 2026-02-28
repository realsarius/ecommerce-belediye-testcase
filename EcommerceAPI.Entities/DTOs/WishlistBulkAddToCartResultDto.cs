using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Entities.DTOs;

public class WishlistBulkAddToCartResultDto : IDto
{
    public int RequestedCount { get; set; }
    public int AddedCount { get; set; }
    public int SkippedCount { get; set; }
    public List<WishlistBulkAddToCartSkippedItemDto> SkippedItems { get; set; } = new();
}

public class WishlistBulkAddToCartSkippedItemDto : IDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}
