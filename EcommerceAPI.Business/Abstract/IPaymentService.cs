using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Core.Utilities.Results;

namespace EcommerceAPI.Business.Abstract;

public interface IPaymentService
{
    Task<IDataResult<PaymentDto>> ProcessPaymentAsync(int userId, ProcessPaymentRequest request);
    Task<IDataResult<PaymentDto>> GetPaymentByOrderIdAsync(int orderId);

    Task<IResult> ProcessWebhookAsync(IyzicoWebhookRequest request, string signatureHeader);
    
    Task<IResult> VerifyAndFinalizePaymentAsync(string token, string conversationId);
}

