using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Entities.DTOs;

public class ReturnRequestItemDto : IDto
{
    public int OrderItemId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }
}
