using EcommerceAPI.Core.DTOs;

namespace EcommerceAPI.Business.Services.Abstract;

public interface ICartService
{
    Task<CartDto> GetCartAsync(int userId);
    Task<CartDto> AddToCartAsync(int userId, AddToCartRequest request);
    Task<CartDto> UpdateCartItemAsync(int userId, int productId, UpdateCartItemRequest request);
    Task<CartDto> RemoveFromCartAsync(int userId, int productId);
    Task<bool> ClearCartAsync(int userId);
}
