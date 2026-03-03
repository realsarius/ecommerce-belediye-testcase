using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Entities.DTOs;

public class UpdateProductRequest : IDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "TRY";
    public string SKU { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public bool IsActive { get; set; } = true;
    public int? StockQuantity { get; set; }
    public List<ProductImageInputDto> Images { get; set; } = new();
    public List<ProductVariantInputDto> Variants { get; set; } = new();
}
