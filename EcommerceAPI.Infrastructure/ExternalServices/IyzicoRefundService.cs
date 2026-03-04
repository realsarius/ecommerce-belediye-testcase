using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Core.CrossCuttingConcerns.Logging;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;
using EcommerceAPI.Infrastructure.Utilities;
using Microsoft.Extensions.Logging;
using EcommerceAPI.Core.CrossCuttingConcerns;

namespace EcommerceAPI.Infrastructure.ExternalServices;

public class IyzicoRefundService : IRefundService, IRefundProvider
{
    public PaymentProviderType ProviderType => PaymentProviderType.Iyzico;

    private readonly IRefundRequestDal _refundRequestDal;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIyzicoRefundGateway _refundGateway;
    private readonly ILoyaltyService _loyaltyService;
    private readonly IGiftCardService _giftCardService;
    private readonly IReferralService _referralService;
    private readonly IAuditService _auditService;
    private readonly ILogger<IyzicoRefundService> _logger;
    private readonly ICorrelationIdProvider _correlationIdProvider;

    public IyzicoRefundService(
        IRefundRequestDal refundRequestDal,
        IUnitOfWork unitOfWork,
        IIyzicoRefundGateway refundGateway,
        ILoyaltyService loyaltyService,
        IGiftCardService giftCardService,
        IReferralService referralService,
        IAuditService auditService,
        ILogger<IyzicoRefundService> logger,
        ICorrelationIdProvider correlationIdProvider)
    {
        _refundRequestDal = refundRequestDal;
        _unitOfWork = unitOfWork;
        _refundGateway = refundGateway;
        _loyaltyService = loyaltyService;
        _giftCardService = giftCardService;
        _referralService = referralService;
        _auditService = auditService;
        _logger = logger;
        _correlationIdProvider = correlationIdProvider;
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
                var sanitizedGatewayError = SensitiveDataLogSanitizer.Sanitize(gatewayResult.ErrorMessage);

                _refundRequestDal.Update(refundRequest);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogWarning(
                    "Refund failed. RefundRequestId={RefundRequestId}, OrderId={OrderId}, ErrorCode={ErrorCode}, ErrorMessage={ErrorMessage}, CorrelationId={CorrelationId}",
                    refundRequest.Id,
                    refundRequest.OrderId,
                    gatewayResult.ErrorCode,
                    sanitizedGatewayError,
                    _correlationIdProvider.GetCorrelationId());

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

            if (refundRequest.Order.LoyaltyPointsUsed > 0)
            {
                var restoreResult = await _loyaltyService.RestoreRedeemedPointsAsync(
                    refundRequest.ReturnRequest.UserId,
                    refundRequest.OrderId,
                    $"Refund sonrası kullanılan puan iadesi ({refundRequest.Order.OrderNumber})");

                if (!restoreResult.Success)
                {
                    return new ErrorDataResult<RefundRequestDto>(restoreResult.Message);
                }
            }

            if (refundRequest.Order.LoyaltyPointsEarned > 0)
            {
                var reverseResult = await _loyaltyService.ReverseEarnedPointsAsync(
                    refundRequest.ReturnRequest.UserId,
                    refundRequest.OrderId,
                    $"Refund sonrası kazanılan puan geri alındı ({refundRequest.Order.OrderNumber})");

                if (!reverseResult.Success)
                {
                    return new ErrorDataResult<RefundRequestDto>(reverseResult.Message);
                }
            }

            if (refundRequest.Order.GiftCardAmount > 0)
            {
                var giftCardRestoreResult = await _giftCardService.RestoreForOrderAsync(
                    refundRequest.ReturnRequest.UserId,
                    refundRequest.OrderId,
                    $"Refund sonrası kullanılan gift card iadesi ({refundRequest.Order.OrderNumber})");

                if (!giftCardRestoreResult.Success)
                {
                    return new ErrorDataResult<RefundRequestDto>(giftCardRestoreResult.Message);
                }
            }

            var referralReverseResult = await _referralService.ReverseRewardsForOrderAsync(
                refundRequest.OrderId,
                $"Refund sonrası referral ödülleri geri alındı ({refundRequest.Order.OrderNumber})");

            if (!referralReverseResult.Success)
            {
                return new ErrorDataResult<RefundRequestDto>(referralReverseResult.Message);
            }

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
                "Refund processed successfully. RefundRequestId={RefundRequestId}, ReturnRequestId={ReturnRequestId}, OrderId={OrderId}, Amount={Amount}, ProviderRefundId={ProviderRefundId}, CorrelationId={CorrelationId}",
                refundRequest.Id,
                refundRequest.ReturnRequestId,
                refundRequest.OrderId,
                refundRequest.Amount,
                refundRequest.ProviderRefundId,
                _correlationIdProvider.GetCorrelationId());

            return new SuccessDataResult<RefundRequestDto>(MapToDto(refundRequest), "Refund işlemi tamamlandı.");
        }
        catch (Exception ex)
        {
            refundRequest.Status = RefundRequestStatus.Pending;
            refundRequest.FailureReason = ex.Message;
            refundRequest.ProcessedAt = null;
            var sanitizedExceptionMessage = SensitiveDataLogSanitizer.Sanitize(ex.Message);

            _refundRequestDal.Update(refundRequest);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogError(
                "Refund processing failed with transient error. RefundRequestId={RefundRequestId}, OrderId={OrderId}, ErrorType={ErrorType}, ErrorMessage={ErrorMessage}, CorrelationId={CorrelationId}",
                refundRequest.Id,
                refundRequest.OrderId,
                ex.GetType().Name,
                sanitizedExceptionMessage,
                _correlationIdProvider.GetCorrelationId());

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
            Provider = refundRequest.Provider,
            Status = refundRequest.Status.ToString(),
            ProviderRefundId = refundRequest.ProviderRefundId,
            FailureReason = refundRequest.FailureReason,
            ProcessedAt = refundRequest.ProcessedAt,
            CreatedAt = refundRequest.CreatedAt
        };
    }
}
