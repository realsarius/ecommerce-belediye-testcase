using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Core.Utilities.Results;

namespace EcommerceAPI.Business.Abstract;

/// <summary>
/// Ödeme işlemleri için servis interface'i.
/// 
/// Not: Bu interface Business katmanında yer alır çünkü DTO'lara bağımlıdır.
/// Implementasyon Infrastructure katmanında (IyzicoPaymentService) yapılır.
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Ödeme işlemini gerçekleştirir.
    /// </summary>
    Task<IDataResult<PaymentDto>> ProcessPaymentAsync(int userId, ProcessPaymentRequest request);
    
    /// <summary>
    /// Sipariş ID'sine göre ödeme bilgisini getirir.
    /// </summary>
    Task<IDataResult<PaymentDto>> GetPaymentByOrderIdAsync(int orderId);

    /// <summary>
    /// Ödeme sağlayıcısından gelen webhook'u işler.
    /// </summary>
    Task<IResult> ProcessWebhookAsync(IyzicoWebhookRequest request, string signatureHeader);
    
    /// <summary>
    /// Ödemeyi doğrular ve sonuçlandırır (3D Secure callback sonrası).
    /// </summary>
    Task<IResult> VerifyAndFinalizePaymentAsync(string token, string conversationId);
}
