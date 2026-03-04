using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Infrastructure.ExternalServices;

public class PayTrPaymentProvider : IPaymentProvider
{
    public PaymentProviderType ProviderType => PaymentProviderType.PayTR;

    public Task<IDataResult<PaymentDto>> ProcessPaymentAsync(int userId, ProcessPaymentRequest request)
    {
        _ = userId;
        _ = request;
        return Task.FromResult<IDataResult<PaymentDto>>(new ErrorDataResult<PaymentDto>(
            "PayTR odeme saglayicisi bu ortamda henuz aktif degil."));
    }

    public Task<IDataResult<PaymentDto>> GetPaymentByOrderIdAsync(int orderId)
    {
        _ = orderId;
        return Task.FromResult<IDataResult<PaymentDto>>(new ErrorDataResult<PaymentDto>(
            "PayTR odeme kaydi sorgulama bu ortamda henuz aktif degil."));
    }

    public Task<IResult> ProcessWebhookAsync(IyzicoWebhookRequest request, string signatureHeader)
    {
        _ = request;
        _ = signatureHeader;
        return Task.FromResult<IResult>(new ErrorResult(
            "PayTR webhook akisi bu ortamda henuz aktif degil."));
    }

    public Task<IResult> VerifyAndFinalizePaymentAsync(string paymentId, string conversationId, string conversationData)
    {
        _ = paymentId;
        _ = conversationId;
        _ = conversationData;
        return Task.FromResult<IResult>(new ErrorResult(
            "PayTR 3D Secure dogrulama akisi bu ortamda henuz aktif degil."));
    }
}
