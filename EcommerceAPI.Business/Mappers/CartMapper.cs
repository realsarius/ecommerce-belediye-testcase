using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Concrete;

namespace EcommerceAPI.Business.Mappers;

public class CartMapper : ICartMapper
{
    public CartDto MapToDto(Cart cart)
    {
        var items = cart.Items.Select(MapToItemDto).ToList();

        return new CartDto
        {
            Id = cart.Id,
            Items = items,
            TotalAmount = items.Sum(i => i.TotalPrice),
            TotalItems = items.Sum(i => i.Quantity)
        };
    }

    public CartItemDto MapToItemDto(CartItem cartItem)
    {
        return new CartItemDto
        {
            Id = cartItem.Id,
            ProductId = cartItem.ProductId,
            ProductName = cartItem.Product?.Name ?? string.Empty,
            ProductSKU = cartItem.Product?.SKU ?? string.Empty,
            Quantity = cartItem.Quantity,
            UnitPrice = cartItem.PriceSnapshot,
            TotalPrice = cartItem.PriceSnapshot * cartItem.Quantity,
            AvailableStock = cartItem.Product?.Inventory?.QuantityAvailable ?? 0
        };
    }
}
