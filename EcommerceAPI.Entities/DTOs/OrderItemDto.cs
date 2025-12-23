using EcommerceAPI.Core.Entities;
namespace EcommerceAPI.Entities.DTOs;

public class OrderItemDto : IDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSKU { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal PriceSnapshot { get; set; }
    public decimal LineTotal { get; set; }
}
