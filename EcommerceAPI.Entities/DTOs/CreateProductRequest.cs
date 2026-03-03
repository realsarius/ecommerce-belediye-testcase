using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Entities.DTOs;

public class CreateProductRequest : IDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "TRY";
    public string SKU { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int CategoryId { get; set; }
    public int InitialStock { get; set; } = 0;
    public List<ProductImageInputDto> Images { get; set; } = new();
    public List<ProductVariantInputDto> Variants { get; set; } = new();
}

public class ProductImageInputDto : IDto
{
    public string ImageUrl { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsPrimary { get; set; }
}

public class ProductVariantInputDto : IDto
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
