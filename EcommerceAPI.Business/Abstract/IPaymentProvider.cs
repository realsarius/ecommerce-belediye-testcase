using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Business.Abstract;

public interface IPaymentProvider
{
    PaymentProviderType ProviderType { get; }

    Task<IDataResult<PaymentDto>> ProcessPaymentAsync(int userId, ProcessPaymentRequest request);
    Task<IDataResult<PaymentDto>> GetPaymentByOrderIdAsync(int orderId);
    Task<IResult> ProcessWebhookAsync(IyzicoWebhookRequest request, string signatureHeader);
    Task<IResult> VerifyAndFinalizePaymentAsync(string token, string conversationId);
}
