using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Core.Utilities.Results;

namespace EcommerceAPI.Business.Abstract;

public interface ICreditCardService
{
    Task<IDataResult<List<CreditCardDto>>> GetUserCardsAsync(int userId);
    Task<IDataResult<CreditCardDto>> AddCardAsync(int userId, AddCreditCardRequest request);
    Task<IResult> DeleteCardAsync(int userId, int cardId);
    Task<IResult> SetDefaultCardAsync(int userId, int cardId);
    
    /// <summary>
    /// Ödeme işlemi için kart bilgilerini şifresi çözülmüş halde döner.
    /// Güvenlik: Sadece kart sahibi kullanıcı için çalışır.
    /// </summary>
    Task<IDataResult<DecryptedCardDto>> GetDecryptedCardForPaymentAsync(int userId, int cardId);
}
