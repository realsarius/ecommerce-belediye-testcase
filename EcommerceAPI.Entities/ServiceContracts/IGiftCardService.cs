using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Entities.DTOs;

namespace EcommerceAPI.Business.Abstract;

public interface IGiftCardService
{
    Task<IDataResult<List<GiftCardDto>>> GetAllAsync();
    Task<IDataResult<GiftCardDto>> GetByIdAsync(int id);
    Task<IDataResult<GiftCardDto>> CreateAsync(CreateGiftCardRequest request);
    Task<IDataResult<GiftCardDto>> UpdateAsync(int id, UpdateGiftCardRequest request);
    Task<IDataResult<List<GiftCardDto>>> GetUserGiftCardsAsync(int userId);
    Task<IDataResult<GiftCardSummaryDto>> GetSummaryAsync(int userId, int recentLimit = 10);
    Task<IDataResult<List<GiftCardTransactionDto>>> GetHistoryAsync(int userId, int limit = 50);
    Task<IDataResult<GiftCardValidationResult>> ValidateAsync(int userId, string code, decimal orderTotal);
    Task<IResult> RedeemForOrderAsync(int userId, int orderId, string code, decimal amount, string description);
    Task<IResult> RestoreForOrderAsync(int userId, int orderId, string description);
}
