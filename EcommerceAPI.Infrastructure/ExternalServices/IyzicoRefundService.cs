using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Core.CrossCuttingConcerns.Logging;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;
using Microsoft.Extensions.Logging;

namespace EcommerceAPI.Infrastructure.ExternalServices;

public class IyzicoRefundService : IRefundService
{
    private readonly IRefundRequestDal _refundRequestDal;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIyzicoRefundGateway _refundGateway;
    private readonly IAuditService _auditService;
    private readonly ILogger<IyzicoRefundService> _logger;

    public IyzicoRefundService(
        IRefundRequestDal refundRequestDal,
        IUnitOfWork unitOfWork,
        IIyzicoRefundGateway refundGateway,
        IAuditService auditService,
        ILogger<IyzicoRefundService> logger)
    {
        _refundRequestDal = refundRequestDal;
        _unitOfWork = unitOfWork;
        _refundGateway = refundGateway;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<IDataResult<RefundRequestDto>> ProcessRefundAsync(int refundRequestId, CancellationToken cancellationToken = default)
    {
        var refundRequest = await _refundRequestDal.GetByIdWithDetailsAsync(refundRequestId);
        if (refundRequest == null)
        {
            return new ErrorDataResult<RefundRequestDto>("Refund talebi bulunamadı.");
        }

        if (refundRequest.Status == RefundRequestStatus.Succeeded)
        {
            return new SuccessDataResult<RefundRequestDto>(MapToDto(refundRequest), "Refund zaten işlendi.");
        }

        if (refundRequest.Payment == null || string.IsNullOrWhiteSpace(refundRequest.Payment.PaymentProviderId))
        {
            refundRequest.Status = RefundRequestStatus.Failed;
            refundRequest.FailureReason = "Ödeme sağlayıcı referansı bulunamadı.";
            refundRequest.ProcessedAt = DateTime.UtcNow;
            _refundRequestDal.Update(refundRequest);
            await _unitOfWork.SaveChangesAsync();

            return new ErrorDataResult<RefundRequestDto>(MapToDto(refundRequest), refundRequest.FailureReason);
        }

        refundRequest.Status = RefundRequestStatus.Processing;
        refundRequest.FailureReason = null;
        _refundRequestDal.Update(refundRequest);
        await _unitOfWork.SaveChangesAsync();

        try
        {
            var gatewayResult = await _refundGateway.RefundAsync(
                new IyzicoRefundGatewayRequest(
                    refundRequest.Payment.PaymentProviderId,
                    refundRequest.Amount,
                    refundRequest.Payment.Currency,
                    "127.0.0.1"),
                cancellationToken);

            if (!gatewayResult.Success)
            {
                refundRequest.Status = RefundRequestStatus.Failed;
                refundRequest.FailureReason = gatewayResult.ErrorMessage ?? "Refund işlemi başarısız oldu.";
                refundRequest.ProcessedAt = DateTime.UtcNow;

                _refundRequestDal.Update(refundRequest);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogWarning(
                    "Refund failed. RefundRequestId={RefundRequestId}, OrderId={OrderId}, ErrorCode={ErrorCode}, ErrorMessage={ErrorMessage}",
                    refundRequest.Id,
                    refundRequest.OrderId,
                    gatewayResult.ErrorCode,
                    gatewayResult.ErrorMessage);

                return new ErrorDataResult<RefundRequestDto>(MapToDto(refundRequest), refundRequest.FailureReason, gatewayResult.ErrorCode);
            }

            refundRequest.Status = RefundRequestStatus.Succeeded;
            refundRequest.ProviderRefundId = gatewayResult.ProviderReferenceId;
            refundRequest.ProcessedAt = DateTime.UtcNow;
            refundRequest.FailureReason = null;

            refundRequest.Payment.Status = PaymentStatus.Refunded;
            refundRequest.Payment.ErrorMessage = null;
            refundRequest.Order.Status = OrderStatus.Refunded;
            refundRequest.ReturnRequest.Status = ReturnRequestStatus.Refunded;

            _refundRequestDal.Update(refundRequest);
            await _unitOfWork.SaveChangesAsync();

            await _auditService.LogActionAsync(
                refundRequest.ReturnRequest.UserId.ToString(),
                "ProcessRefund",
                "RefundRequest",
                new
                {
                    refundRequest.Id,
                    refundRequest.ReturnRequestId,
                    refundRequest.OrderId,
                    refundRequest.Amount,
                    ProviderRefundId = refundRequest.ProviderRefundId
                });

            _logger.LogInformation(
                "Refund processed successfully. RefundRequestId={RefundRequestId}, ReturnRequestId={ReturnRequestId}, OrderId={OrderId}, Amount={Amount}, ProviderRefundId={ProviderRefundId}",
                refundRequest.Id,
                refundRequest.ReturnRequestId,
                refundRequest.OrderId,
                refundRequest.Amount,
                refundRequest.ProviderRefundId);

            return new SuccessDataResult<RefundRequestDto>(MapToDto(refundRequest), "Refund işlemi tamamlandı.");
        }
        catch (Exception ex)
        {
            refundRequest.Status = RefundRequestStatus.Pending;
            refundRequest.FailureReason = ex.Message;
            refundRequest.ProcessedAt = null;

            _refundRequestDal.Update(refundRequest);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogError(
                ex,
                "Refund processing failed with transient error. RefundRequestId={RefundRequestId}, OrderId={OrderId}",
                refundRequest.Id,
                refundRequest.OrderId);

            throw;
        }
    }

    private static RefundRequestDto MapToDto(Entities.Concrete.RefundRequest refundRequest)
    {
        return new RefundRequestDto
        {
            Id = refundRequest.Id,
            ReturnRequestId = refundRequest.ReturnRequestId,
            OrderId = refundRequest.OrderId,
            UserId = refundRequest.ReturnRequest.UserId,
            OrderNumber = refundRequest.Order.OrderNumber,
            CustomerEmail = refundRequest.ReturnRequest.User.Email,
            CustomerName = $"{refundRequest.ReturnRequest.User.FirstName} {refundRequest.ReturnRequest.User.LastName}".Trim(),
            Amount = refundRequest.Amount,
            Currency = refundRequest.Payment?.Currency ?? refundRequest.Order.Currency,
            Status = refundRequest.Status.ToString(),
            ProviderRefundId = refundRequest.ProviderRefundId,
            FailureReason = refundRequest.FailureReason,
            ProcessedAt = refundRequest.ProcessedAt,
            CreatedAt = refundRequest.CreatedAt
        };
    }
}
