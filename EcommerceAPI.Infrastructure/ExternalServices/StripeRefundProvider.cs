using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Infrastructure.ExternalServices;

public class StripeRefundProvider : IRefundProvider
{
    public PaymentProviderType ProviderType => PaymentProviderType.Stripe;

    public Task<IDataResult<RefundRequestDto>> ProcessRefundAsync(int refundRequestId, CancellationToken cancellationToken = default)
    {
        _ = refundRequestId;
        _ = cancellationToken;
        return Task.FromResult<IDataResult<RefundRequestDto>>(new ErrorDataResult<RefundRequestDto>(
            "Stripe refund akisi bu ortamda henuz aktif degil."));
    }
}
