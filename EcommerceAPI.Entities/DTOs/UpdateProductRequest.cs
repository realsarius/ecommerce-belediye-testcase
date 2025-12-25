using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Entities.DTOs;

public class UpdateProductRequest : IDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int CategoryId { get; set; }
    public bool IsActive { get; set; } = true;
    public int? StockQuantity { get; set; }
}
