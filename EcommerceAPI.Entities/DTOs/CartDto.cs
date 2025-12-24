using EcommerceAPI.Core.Entities;
namespace EcommerceAPI.Entities.DTOs;

public class CartDto : IDto
{
    public int Id { get; set; }
    public List<CartItemDto> Items { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public int TotalItems { get; set; }
}
