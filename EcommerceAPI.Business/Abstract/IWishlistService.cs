using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Entities.DTOs;
using System.Threading.Tasks;

namespace EcommerceAPI.Business.Abstract;

public interface IWishlistService
{
    Task<IDataResult<WishlistDto>> GetWishlistByUserIdAsync(int userId, string? cursor = null, int? limit = null);
    Task<IResult> AddItemToWishlistAsync(int userId, int productId);
    Task<IResult> RemoveItemFromWishlistAsync(int userId, int productId);
    Task<IResult> ClearWishlistAsync(int userId);
}
