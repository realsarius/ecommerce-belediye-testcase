using EcommerceAPI.Core.Entities;
namespace EcommerceAPI.Entities.DTOs;

public class CartItemDto : IDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSKU { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public int AvailableStock { get; set; }
}
