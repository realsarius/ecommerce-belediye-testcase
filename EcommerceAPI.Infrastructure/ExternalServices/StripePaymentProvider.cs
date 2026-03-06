using EcommerceAPI.Application.Abstractions.ServiceContracts;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Infrastructure.ExternalServices;

public class StripePaymentProvider : IPaymentProvider
{
    public PaymentProviderType ProviderType => PaymentProviderType.Stripe;

    public Task<IDataResult<PaymentDto>> ProcessPaymentAsync(int userId, ProcessPaymentRequest request)
    {
        _ = userId;
        _ = request;
        return Task.FromResult<IDataResult<PaymentDto>>(new ErrorDataResult<PaymentDto>(
            "Stripe odeme saglayicisi bu ortamda henuz aktif degil."));
    }

    public Task<IDataResult<PaymentDto>> GetPaymentByOrderIdAsync(int orderId)
    {
        _ = orderId;
        return Task.FromResult<IDataResult<PaymentDto>>(new ErrorDataResult<PaymentDto>(
            "Stripe odeme kaydi sorgulama bu ortamda henuz aktif degil."));
    }

    public Task<IResult> ProcessWebhookAsync(IyzicoWebhookRequest request, string signatureHeader)
    {
        _ = request;
        _ = signatureHeader;
        return Task.FromResult<IResult>(new ErrorResult(
            "Stripe webhook akisi bu ortamda henuz aktif degil."));
    }

    public Task<IResult> VerifyAndFinalizePaymentAsync(string paymentId, string conversationId, string conversationData)
    {
        _ = paymentId;
        _ = conversationId;
        _ = conversationData;
        return Task.FromResult<IResult>(new ErrorResult(
            "Stripe 3D Secure dogrulama akisi bu ortamda henuz aktif degil."));
    }
}
