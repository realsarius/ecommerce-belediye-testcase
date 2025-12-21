using EcommerceAPI.Core.DTOs;

namespace EcommerceAPI.Business.Services.Abstract;

public interface IPaymentService
{
    Task<PaymentDto> ProcessPaymentAsync(int userId, ProcessPaymentRequest request);
    Task<PaymentDto?> GetPaymentByOrderIdAsync(int orderId);

    Task<bool> ProcessWebhookAsync(IyzicoWebhookRequest request, string signatureHeader);
    
    Task<bool> VerifyAndFinalizePaymentAsync(string token, string conversationId);
}
