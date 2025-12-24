using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Entities.DTOs;

public class UpdateCartItemRequest : IDto
{
    public int Quantity { get; set; }
}
