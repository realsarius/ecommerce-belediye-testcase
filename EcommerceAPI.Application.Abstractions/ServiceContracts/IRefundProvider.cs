using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Application.Abstractions.ServiceContracts;

public interface IRefundProvider
{
    PaymentProviderType ProviderType { get; }

    Task<IDataResult<RefundRequestDto>> ProcessRefundAsync(int refundRequestId, CancellationToken cancellationToken = default);
}
