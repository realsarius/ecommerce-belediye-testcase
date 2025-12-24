using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Entities.DTOs;

public class AddToCartRequest : IDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; } = 1;
}
