using EcommerceAPI.Core.DTOs;
using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Business.Mappers;

public interface ICartMapper
{
    CartDto MapToDto(Cart cart);
    CartItemDto MapToItemDto(CartItem cartItem);
}
