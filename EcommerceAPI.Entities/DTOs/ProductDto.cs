using EcommerceAPI.Core.Entities;
namespace EcommerceAPI.Entities.DTOs;

public class ProductDto : IDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "TRY";
    public string SKU { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public int? SellerId { get; set; }
    public string? SellerBrandName { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public int WishlistCount { get; set; }
}
