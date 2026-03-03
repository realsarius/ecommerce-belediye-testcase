using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Business.Constants;
using EcommerceAPI.Business.Validators;
using EcommerceAPI.Core.Aspects.Autofac.Logging;
using EcommerceAPI.Core.Aspects.Autofac.Transaction;
using EcommerceAPI.Core.Aspects.Autofac.Validation;
using EcommerceAPI.Core.CrossCuttingConcerns.Logging;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;
using EcommerceAPI.Entities.IntegrationEvents;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EcommerceAPI.Business.Concrete;

public class ReturnRequestManager : IReturnRequestService
{
    private readonly IReturnRequestDal _returnRequestDal;
    private readonly IRefundRequestDal _refundRequestDal;
    private readonly IOrderDal _orderDal;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILoyaltyService _loyaltyService;
    private readonly IGiftCardService _giftCardService;
    private readonly IReferralService _referralService;
    private readonly IAuditService _auditService;
    private readonly ILogger<ReturnRequestManager> _logger;
    private readonly IPublishEndpoint _publishEndpoint;

    public ReturnRequestManager(
        IReturnRequestDal returnRequestDal,
        IRefundRequestDal refundRequestDal,
        IOrderDal orderDal,
        IUnitOfWork unitOfWork,
        ILoyaltyService loyaltyService,
        IGiftCardService giftCardService,
        IReferralService referralService,
        IAuditService auditService,
        ILogger<ReturnRequestManager> logger,
        IPublishEndpoint publishEndpoint)
    {
        _returnRequestDal = returnRequestDal;
        _refundRequestDal = refundRequestDal;
        _orderDal = orderDal;
        _unitOfWork = unitOfWork;
        _loyaltyService = loyaltyService;
        _giftCardService = giftCardService;
        _referralService = referralService;
        _auditService = auditService;
        _logger = logger;
        _publishEndpoint = publishEndpoint;
    }

    [LogAspect]
    public async Task<IDataResult<List<ReturnRequestDto>>> GetUserReturnRequestsAsync(int userId)
    {
        var requests = await _returnRequestDal.GetUserRequestsAsync(userId);
        return new SuccessDataResult<List<ReturnRequestDto>>(requests.Select(MapToDto).ToList());
    }

    [LogAspect]
    public async Task<IDataResult<List<ReturnRequestDto>>> GetReturnRequestsAsync(string? status = null, int? sellerId = null)
    {
        ReturnRequestStatus? parsedStatus = null;

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<ReturnRequestStatus>(status, true, out var parsed))
            {
                return new ErrorDataResult<List<ReturnRequestDto>>("Geçersiz iade talebi durumu.");
            }

            parsedStatus = parsed;
        }

        var requests = await _returnRequestDal.GetListWithDetailsAsync(parsedStatus, sellerId);
        return new SuccessDataResult<List<ReturnRequestDto>>(requests.Select(MapToDto).ToList());
    }

    [LogAspect]
    [TransactionScopeAspect]
    [ValidationAspect(typeof(CreateReturnRequestRequestValidator))]
    public async Task<IDataResult<ReturnRequestDto>> CreateReturnRequestAsync(int userId, int orderId, CreateReturnRequestRequest request)
    {
        var order = await _orderDal.GetByIdWithDetailsAsync(orderId);
        if (order == null || order.UserId != userId)
        {
            return new ErrorDataResult<ReturnRequestDto>(Messages.OrderNotFound);
        }

        if (await _returnRequestDal.HasActiveRequestForOrderAsync(orderId))
        {
            return new ErrorDataResult<ReturnRequestDto>("Bu sipariş için zaten aktif bir iade veya iptal talebi bulunuyor.");
        }

        if (!TryParseReturnRequestType(request.Type, out var requestType))
        {
            return new ErrorDataResult<ReturnRequestDto>("Geçersiz talep tipi.");
        }

        if (!TryParseReasonCategory(request.ReasonCategory, out var reasonCategory))
        {
            return new ErrorDataResult<ReturnRequestDto>("Geçersiz talep kategorisi.");
        }

        if (!IsRequestAllowed(order, requestType))
        {
            return new ErrorDataResult<ReturnRequestDto>(GetInvalidStateMessage(order, requestType));
        }

        var returnRequest = new ReturnRequest
        {
            OrderId = orderId,
            UserId = userId,
            Type = requestType,
            ReasonCategory = reasonCategory,
            Status = ReturnRequestStatus.Pending,
            Reason = request.Reason.Trim(),
            RequestNote = string.IsNullOrWhiteSpace(request.RequestNote) ? null : request.RequestNote.Trim(),
            RequestedRefundAmount = order.Payment?.Status == PaymentStatus.Success ? order.TotalAmount : 0m
        };

        await _returnRequestDal.AddAsync(returnRequest);
        await _unitOfWork.SaveChangesAsync();

        await _auditService.LogActionAsync(
            userId.ToString(),
            "CreateReturnRequest",
            "ReturnRequest",
            new
            {
                OrderId = orderId,
                returnRequest.Type,
                returnRequest.ReasonCategory,
                returnRequest.Reason,
                returnRequest.RequestedRefundAmount
            });

        _logger.LogInformation(
            "Return request created. OrderId={OrderId}, UserId={UserId}, Type={Type}, ReasonCategory={ReasonCategory}, RequestedRefundAmount={RequestedRefundAmount}",
            orderId,
            userId,
            returnRequest.Type,
            returnRequest.ReasonCategory,
            returnRequest.RequestedRefundAmount);

        var createdRequest = await _returnRequestDal.GetByIdWithDetailsAsync(returnRequest.Id) ?? returnRequest;
        return new SuccessDataResult<ReturnRequestDto>(MapToDto(createdRequest), "İade / iptal talebi oluşturuldu.");
    }

    [LogAspect]
    [TransactionScopeAspect]
    [ValidationAspect(typeof(ReviewReturnRequestRequestValidator))]
    public async Task<IDataResult<ReturnRequestDto>> ReviewReturnRequestAsync(int requestId, int reviewerUserId, ReviewReturnRequestRequest request, int? sellerId = null)
    {
        var returnRequest = await _returnRequestDal.GetByIdWithDetailsAsync(requestId);
        if (returnRequest == null)
        {
            return new ErrorDataResult<ReturnRequestDto>("İade / iptal talebi bulunamadı.");
        }

        if (sellerId.HasValue && !returnRequest.Order.OrderItems.Any(item => item.Product.SellerId == sellerId.Value))
        {
            return new ErrorDataResult<ReturnRequestDto>(Messages.OrderNotBelongToUser);
        }

        if (returnRequest.Status != ReturnRequestStatus.Pending)
        {
            return new ErrorDataResult<ReturnRequestDto>("Sadece bekleyen talepler değerlendirilebilir.");
        }

        if (!TryParseReviewStatus(request.Status, out var targetStatus))
        {
            return new ErrorDataResult<ReturnRequestDto>("Geçersiz karar durumu.");
        }

        returnRequest.ReviewedByUserId = reviewerUserId;
        returnRequest.ReviewedAt = DateTime.UtcNow;
        returnRequest.ReviewNote = string.IsNullOrWhiteSpace(request.ReviewNote) ? null : request.ReviewNote.Trim();

        RefundRequest? createdRefundRequest = null;

        if (targetStatus == ReturnRequestStatus.Rejected)
        {
            returnRequest.Status = ReturnRequestStatus.Rejected;
        }
        else if (returnRequest.Order.Payment?.Status == PaymentStatus.Success && returnRequest.RequestedRefundAmount > 0)
        {
            returnRequest.Status = ReturnRequestStatus.RefundPending;

            var existingRefundRequest = await _refundRequestDal.GetByReturnRequestIdAsync(returnRequest.Id);
            if (existingRefundRequest == null)
            {
                createdRefundRequest = new RefundRequest
                {
                    ReturnRequestId = returnRequest.Id,
                    OrderId = returnRequest.OrderId,
                    PaymentId = returnRequest.Order.Payment.Id,
                    Amount = returnRequest.RequestedRefundAmount,
                    Status = RefundRequestStatus.Pending,
                    IdempotencyKey = $"refund:{returnRequest.Id}:{Guid.NewGuid():N}"
                };

                await _refundRequestDal.AddAsync(createdRefundRequest);
            }
        }
        else if (returnRequest.Order.Payment?.Status == PaymentStatus.Success && returnRequest.Order.GiftCardAmount > 0)
        {
            if (returnRequest.Order.LoyaltyPointsUsed > 0)
            {
                var loyaltyRestoreResult = await _loyaltyService.RestoreRedeemedPointsAsync(
                    returnRequest.UserId,
                    returnRequest.OrderId,
                    $"İade/iptal onayı sonrası puan iadesi ({returnRequest.Order.OrderNumber})");

                if (!loyaltyRestoreResult.Success)
                {
                    return new ErrorDataResult<ReturnRequestDto>(loyaltyRestoreResult.Message);
                }
            }

            if (returnRequest.Order.LoyaltyPointsEarned > 0)
            {
                var loyaltyReverseResult = await _loyaltyService.ReverseEarnedPointsAsync(
                    returnRequest.UserId,
                    returnRequest.OrderId,
                    $"İade/iptal onayı sonrası kazanılan puanlar geri alındı ({returnRequest.Order.OrderNumber})");

                if (!loyaltyReverseResult.Success)
                {
                    return new ErrorDataResult<ReturnRequestDto>(loyaltyReverseResult.Message);
                }
            }

            var giftCardRestoreResult = await _giftCardService.RestoreForOrderAsync(
                returnRequest.UserId,
                returnRequest.OrderId,
                $"İade/iptal onayı sonrası gift card iadesi ({returnRequest.Order.OrderNumber})");

            if (!giftCardRestoreResult.Success)
            {
                return new ErrorDataResult<ReturnRequestDto>(giftCardRestoreResult.Message);
            }

            var referralReverseResult = await _referralService.ReverseRewardsForOrderAsync(
                returnRequest.OrderId,
                $"İade/iptal onayı sonrası referral ödülleri geri alındı ({returnRequest.Order.OrderNumber})");

            if (!referralReverseResult.Success)
            {
                return new ErrorDataResult<ReturnRequestDto>(referralReverseResult.Message);
            }

            returnRequest.Status = ReturnRequestStatus.Refunded;
            returnRequest.Order.Status = OrderStatus.Refunded;
            returnRequest.Order.Payment.Status = PaymentStatus.Refunded;
            returnRequest.Order.Payment.ErrorMessage = null;
        }
        else
        {
            returnRequest.Status = ReturnRequestStatus.Approved;
        }

        _returnRequestDal.Update(returnRequest);
        await _unitOfWork.SaveChangesAsync();

        if (createdRefundRequest != null)
        {
            await _publishEndpoint.Publish(new RefundRequestedEvent
            {
                RefundRequestId = createdRefundRequest.Id,
                ReturnRequestId = returnRequest.Id,
                OrderId = returnRequest.OrderId,
                UserId = returnRequest.UserId,
                Amount = createdRefundRequest.Amount,
                Currency = returnRequest.Order.Payment?.Currency ?? returnRequest.Order.Currency
            });

            await _unitOfWork.SaveChangesAsync();
        }

        await _publishEndpoint.Publish(new ReturnRequestReviewedEvent
        {
            ReturnRequestId = returnRequest.Id,
            OrderId = returnRequest.OrderId,
            UserId = returnRequest.UserId,
            UserEmail = returnRequest.User.Email,
            CustomerName = $"{returnRequest.User.FirstName} {returnRequest.User.LastName}".Trim(),
            OrderNumber = returnRequest.Order.OrderNumber,
            Decision = targetStatus.ToString(),
            CurrentStatus = returnRequest.Status.ToString(),
            ReviewNote = returnRequest.ReviewNote,
            ReviewedAt = returnRequest.ReviewedAt ?? DateTime.UtcNow
        });

        await _auditService.LogActionAsync(
            reviewerUserId.ToString(),
            "ReviewReturnRequest",
            "ReturnRequest",
            new
            {
                ReturnRequestId = returnRequest.Id,
                returnRequest.OrderId,
                NewStatus = returnRequest.Status.ToString(),
                returnRequest.ReviewedAt
            });

        var reviewedRequest = await _returnRequestDal.GetByIdWithDetailsAsync(returnRequest.Id) ?? returnRequest;
        return new SuccessDataResult<ReturnRequestDto>(MapToDto(reviewedRequest), "Talep değerlendirildi.");
    }

    private static bool TryParseReturnRequestType(string value, out ReturnRequestType requestType)
    {
        return Enum.TryParse(value, true, out requestType);
    }

    private static bool TryParseReasonCategory(string value, out ReturnReasonCategory category)
    {
        return Enum.TryParse(value, true, out category);
    }

    private static bool TryParseReviewStatus(string value, out ReturnRequestStatus status)
    {
        var success = Enum.TryParse(value, true, out status);
        return success && (status == ReturnRequestStatus.Approved || status == ReturnRequestStatus.Rejected);
    }

    private static bool IsRequestAllowed(Order order, ReturnRequestType requestType)
    {
        return requestType switch
        {
            ReturnRequestType.Cancellation => order.Status == OrderStatus.PendingPayment ||
                                              order.Status == OrderStatus.Paid ||
                                              order.Status == OrderStatus.Processing,
            ReturnRequestType.Return => order.Status == OrderStatus.Delivered &&
                                        !IsReturnWindowExpired(order),
            _ => false
        };
    }

    private static bool IsReturnWindowExpired(Order order)
    {
        if (!order.DeliveredAt.HasValue)
        {
            return false;
        }

        return order.DeliveredAt.Value.Date.AddDays(14) < DateTime.UtcNow.Date;
    }

    private static string GetInvalidStateMessage(Order order, ReturnRequestType requestType)
    {
        return requestType switch
        {
            ReturnRequestType.Cancellation => $"Sipariş durumu {order.Status} iken iptal talebi açılamaz.",
            ReturnRequestType.Return when order.Status == OrderStatus.Delivered && IsReturnWindowExpired(order)
                => "İade talebi teslim tarihinden itibaren 14 gün içinde açılabilir.",
            ReturnRequestType.Return => "İade talebi yalnızca teslim edilen siparişler için açılabilir.",
            _ => "Talep oluşturulamadı."
        };
    }

    private static ReturnRequestDto MapToDto(ReturnRequest request)
    {
        return new ReturnRequestDto
        {
            Id = request.Id,
            OrderId = request.OrderId,
            OrderNumber = request.Order?.OrderNumber ?? string.Empty,
            UserId = request.UserId,
            CustomerName = request.User != null ? $"{request.User.FirstName} {request.User.LastName}".Trim() : string.Empty,
            Type = request.Type.ToString(),
            ReasonCategory = request.ReasonCategory.ToString(),
            Status = request.Status.ToString(),
            Reason = request.Reason,
            RequestNote = request.RequestNote,
            RequestedRefundAmount = request.RequestedRefundAmount,
            PaymentStatus = request.Order?.Payment?.Status.ToString(),
            ReviewedByUserId = request.ReviewedByUserId,
            ReviewerName = request.ReviewedByUser != null ? $"{request.ReviewedByUser.FirstName} {request.ReviewedByUser.LastName}".Trim() : null,
            ReviewNote = request.ReviewNote,
            ReviewedAt = request.ReviewedAt,
            RefundRequestId = request.RefundRequest?.Id,
            RefundStatus = request.RefundRequest?.Status.ToString(),
            CreatedAt = request.CreatedAt
        };
    }
}
