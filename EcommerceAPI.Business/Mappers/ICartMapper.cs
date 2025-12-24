using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Concrete;

namespace EcommerceAPI.Business.Mappers;

public interface ICartMapper
{
    CartDto MapToDto(Cart cart);
    CartItemDto MapToItemDto(CartItem cartItem);
}
