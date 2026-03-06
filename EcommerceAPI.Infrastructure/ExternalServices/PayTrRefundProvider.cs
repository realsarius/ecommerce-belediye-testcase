using EcommerceAPI.Application.Abstractions.ServiceContracts;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Infrastructure.ExternalServices;

public class PayTrRefundProvider : IRefundProvider
{
    public PaymentProviderType ProviderType => PaymentProviderType.PayTR;

    public Task<IDataResult<RefundRequestDto>> ProcessRefundAsync(int refundRequestId, CancellationToken cancellationToken = default)
    {
        _ = refundRequestId;
        _ = cancellationToken;
        return Task.FromResult<IDataResult<RefundRequestDto>>(new ErrorDataResult<RefundRequestDto>(
            "PayTR refund akisi bu ortamda henuz aktif degil."));
    }
}
