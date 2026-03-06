using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Entities.DTOs;

namespace EcommerceAPI.Application.Abstractions.ServiceContracts;

public interface IRefundService
{
    Task<IDataResult<RefundRequestDto>> ProcessRefundAsync(int refundRequestId, CancellationToken cancellationToken = default);
}
