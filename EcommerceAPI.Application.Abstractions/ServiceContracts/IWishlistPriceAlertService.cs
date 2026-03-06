using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Entities.DTOs;

namespace EcommerceAPI.Business.Abstract;

public interface IWishlistPriceAlertService
{
    Task<IDataResult<List<WishlistPriceAlertDto>>> GetUserPriceAlertsAsync(int userId);
    Task<IDataResult<WishlistPriceAlertDto>> UpsertPriceAlertAsync(int userId, UpsertWishlistPriceAlertRequest request);
    Task<IResult> RemovePriceAlertAsync(int userId, int productId);
    Task ProcessPriceAlertsAsync();
}
