using EcommerceAPI.Application.Abstractions.ServiceContracts;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Application.Abstractions.Persistence;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Infrastructure.Services;

public class RefundService : IRefundService
{
    private readonly IRefundProviderFactory _refundProviderFactory;
    private readonly IRefundRequestDal _refundRequestDal;

    public RefundService(
        IRefundProviderFactory refundProviderFactory,
        IRefundRequestDal refundRequestDal)
    {
        _refundProviderFactory = refundProviderFactory;
        _refundRequestDal = refundRequestDal;
    }

    public async Task<IDataResult<RefundRequestDto>> ProcessRefundAsync(int refundRequestId, CancellationToken cancellationToken = default)
    {
        var refundRequest = await _refundRequestDal.GetByIdWithDetailsAsync(refundRequestId);
        if (refundRequest == null)
        {
            return new ErrorDataResult<RefundRequestDto>("Refund talebi bulunamadi.");
        }

        var providerType = refundRequest.Provider;

        try
        {
            return await _refundProviderFactory.GetProvider(providerType)
                .ProcessRefundAsync(refundRequestId, cancellationToken);
        }
        catch (NotSupportedException ex)
        {
            return new ErrorDataResult<RefundRequestDto>(ex.Message);
        }
    }
}
