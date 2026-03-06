using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Core.Utilities.Results;

namespace EcommerceAPI.Application.Abstractions.ServiceContracts;

public interface ICreditCardService
{
    Task<IDataResult<List<CreditCardDto>>> GetUserCardsAsync(int userId);
    Task<IDataResult<CreditCardDto>> AddCardAsync(int userId, AddCreditCardRequest request);
    Task<IDataResult<CreditCardDto>> SaveTokenizedCardAsync(int userId, SaveTokenizedCreditCardRequest request);
    Task<IResult> DeleteCardAsync(int userId, int cardId);
    Task<IResult> SetDefaultCardAsync(int userId, int cardId);
    
    /// <summary>
    /// Ödeme işlemi için kayıtlı kart bilgisini döner.
    /// Güvenlik: Sadece kart sahibi kullanıcı için çalışır.
    /// </summary>
    Task<IDataResult<StoredCardPaymentDto>> GetStoredCardForPaymentAsync(int userId, int cardId);
}
