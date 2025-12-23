using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Core.Utilities.Results;

namespace EcommerceAPI.Business.Abstract;

public interface ICartService
{
    Task<IDataResult<CartDto>> GetCartAsync(int userId);
    Task<IDataResult<CartDto>> AddToCartAsync(int userId, AddToCartRequest request);
    Task<IDataResult<CartDto>> UpdateCartItemAsync(int userId, int productId, UpdateCartItemRequest request);
    Task<IDataResult<CartDto>> RemoveFromCartAsync(int userId, int productId);
    Task<IResult> ClearCartAsync(int userId);
}

