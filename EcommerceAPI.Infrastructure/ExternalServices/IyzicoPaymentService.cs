using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Infrastructure.Settings;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.Enums;
using EcommerceAPI.Entities.Utilities;
using EcommerceAPI.Core.Utilities.Redis;
using EcommerceAPI.Infrastructure.Utilities;
using Iyzipay;
using Iyzipay.Model;
using Iyzipay.Request;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EcommerceAPI.Core.CrossCuttingConcerns;

namespace EcommerceAPI.Infrastructure.ExternalServices;

/// <summary>
/// Iyzico ödeme API entegrasyonu.
/// </summary>
public class IyzicoPaymentService : IPaymentService, IPaymentProvider
{
    public PaymentProviderType ProviderType => PaymentProviderType.Iyzico;

    private sealed class IyzicoPaymentExecutionResult
    {
        public IyzicoChargeGatewayResult? Payment { get; init; }
        public IyzicoThreeDSInitializeGatewayResult? ThreeDSInitialize { get; init; }
        public string? Last4Digits { get; init; }
        public bool RequiresThreeDS => ThreeDSInitialize != null;
    }

    private readonly IOrderDal _orderDal;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IyzicoSettings _settings;
    private readonly PaymentSettings _paymentSettings;
    private readonly ICreditCardService _creditCardService;
    private readonly ILoyaltyService _loyaltyService;
    private readonly IReferralService _referralService;
    private readonly IIyzicoPaymentGateway _paymentGateway;
    private readonly IDistributedLockService _lockService;
    private readonly Microsoft.Extensions.Logging.ILogger<IyzicoPaymentService> _logger;
    private readonly ICorrelationIdProvider _correlationIdProvider;

    public IyzicoPaymentService(
        IOrderDal orderDal,
        IUnitOfWork unitOfWork,
        IOptions<IyzicoSettings> settings,
        IOptions<PaymentSettings> paymentSettings,
        ICreditCardService creditCardService,
        ILoyaltyService loyaltyService,
        IReferralService referralService,
        IIyzicoPaymentGateway paymentGateway,
        IDistributedLockService lockService,
        Microsoft.Extensions.Logging.ILogger<IyzicoPaymentService> logger,
        ICorrelationIdProvider correlationIdProvider)
    {
        _orderDal = orderDal;
        _unitOfWork = unitOfWork;
        _settings = settings.Value;
        _paymentSettings = paymentSettings.Value;
        _creditCardService = creditCardService;
        _loyaltyService = loyaltyService;
        _referralService = referralService;
        _paymentGateway = paymentGateway;
        _lockService = lockService;
        _logger = logger;
        _correlationIdProvider = correlationIdProvider;
    }

    public async Task<IDataResult<PaymentDto>> ProcessPaymentAsync(int userId, ProcessPaymentRequest request)
    {
        var requestedProvider = request.PaymentProvider ?? PaymentProviderType.Iyzico;
        if (requestedProvider != PaymentProviderType.Iyzico)
        {
            return new ErrorDataResult<PaymentDto>(
                $"Secilen odeme saglayicisi henuz aktif degil: {requestedProvider}");
        }

        request.PaymentProvider = requestedProvider;

        _logger.LogInformation(
            "Processing payment request. UserId={UserId}, OrderId={OrderId}, UsesSavedCard={UsesSavedCard}, Provider={Provider}, CorrelationId={CorrelationId}",
            userId,
            request.OrderId,
            request.SavedCardId.HasValue,
            requestedProvider,
            _correlationIdProvider.GetCorrelationId());
        var lockKey = RedisKeys.PaymentLock(request.OrderId);
        var lockToken = await _lockService.TryAcquireLockAsync(lockKey);

        if (lockToken == null)
        {
            return new ErrorDataResult<PaymentDto>(
                Constants.InfrastructureConstants.Redis.SystemBusyMessage,
                Constants.InfrastructureConstants.Redis.SystemBusyCode);
        }

        try
        {
            var order = await _orderDal.GetByIdWithDetailsAsync(request.OrderId);

            if (order == null || order.UserId != userId)
                return new ErrorDataResult<PaymentDto>(
                    Constants.InfrastructureConstants.Payment.OrderNotFoundCode, 
                    $"Sipariş bulunamadı: {request.OrderId}");

            if (order.Payment == null)
                return new ErrorDataResult<PaymentDto>(
                    Constants.InfrastructureConstants.Payment.PaymentRecordNotFoundCode,
                    "Bu siparişe ait ödeme kaydı bulunamadı.");

            var idempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey)
                ? $"{order.OrderNumber}-{userId}-{DateTime.UtcNow:yyyyMMddHHmmss}"
                : request.IdempotencyKey.Trim();

            request.IdempotencyKey = idempotencyKey;

            if (order.Payment.IdempotencyKey == idempotencyKey &&
                order.Payment.Status == PaymentStatus.Success)
            {
                return new SuccessDataResult<PaymentDto>(MapToDto(order.Payment));
            }

            if (order.Payment.Status == PaymentStatus.Success)
                return new ErrorDataResult<PaymentDto>(
                    Constants.InfrastructureConstants.Payment.AlreadyPaidCode,
                    "Bu sipariş için ödeme zaten alınmış.");

            var validationError = ValidatePaymentRequest(request);
            if (validationError != null)
            {
                return new ErrorDataResult<PaymentDto>(validationError);
            }

            var requiresThreeDS = ShouldRequireThreeDS(order.TotalAmount, request);
            if (requiresThreeDS && request.SaveCard && !request.SavedCardId.HasValue)
            {
                return new ErrorDataResult<PaymentDto>(
                    "3D Secure gereken odemelerde yeni karti kaydetme simdilik desteklenmiyor.");
            }

            IyzicoPaymentExecutionResult iyzicoPaymentResult;
            try
            {
                iyzicoPaymentResult = await ProcessIyzicoPaymentAsync(order, request, requiresThreeDS);
            }
            catch (InvalidOperationException ex)
            {
                return new ErrorDataResult<PaymentDto>(ex.Message);
            }

            order.Payment.Last4Digits = iyzicoPaymentResult.Last4Digits;

            if (iyzicoPaymentResult.RequiresThreeDS)
            {
                var threeDSInitialize = iyzicoPaymentResult.ThreeDSInitialize!;

                if (threeDSInitialize.Success && !string.IsNullOrWhiteSpace(threeDSInitialize.HtmlContent))
                {
                    order.Payment.Status = PaymentStatus.Pending;
                    order.Payment.Provider = PaymentProviderType.Iyzico;
                    order.Payment.PaymentProviderId = threeDSInitialize.PaymentId;
                    order.Payment.ErrorMessage = null;
                    order.Payment.IdempotencyKey = idempotencyKey;

                    _orderDal.Update(order);
                    await _unitOfWork.SaveChangesAsync();

                    return new SuccessDataResult<PaymentDto>(new PaymentDto
                    {
                        Id = order.Payment.Id,
                        Amount = order.Payment.Amount,
                        Currency = order.Payment.Currency,
                        Status = order.Payment.Status.ToString(),
                        PaymentMethod = order.Payment.PaymentMethod,
                        Provider = order.Payment.Provider,
                        Last4Digits = order.Payment.Last4Digits,
                        ErrorMessage = null,
                        RequiresThreeDS = true,
                        ThreeDSHtmlContent = threeDSInitialize.HtmlContent,
                        CreatedAt = order.Payment.CreatedAt
                    });
                }

                order.Payment.Status = PaymentStatus.Failed;
                order.Payment.ErrorMessage = threeDSInitialize.ErrorMessage ?? "3D Secure baslatilamadi.";
                order.Payment.IdempotencyKey = idempotencyKey;

                _orderDal.Update(order);
                await _unitOfWork.SaveChangesAsync();
                LogPaymentFailure(order, userId, request, requiresThreeDS, "ThreeDSInitialize", order.Payment.ErrorMessage);

                return new ErrorDataResult<PaymentDto>(
                    MapToDto(order.Payment),
                    order.Payment.ErrorMessage,
                    Constants.InfrastructureConstants.Payment.DefaultErrorCode,
                    threeDSInitialize.ErrorMessage);
            }

            var iyzicoPayment = iyzicoPaymentResult.Payment!;

            if (iyzicoPayment.Success)
            {
                order.Payment.Status = PaymentStatus.Success;
                order.Payment.Provider = PaymentProviderType.Iyzico;
                order.Payment.PaymentProviderId = iyzicoPayment.PaymentId;
                order.Status = OrderStatus.Paid;
                order.LoyaltyPointsEarned = CalculateEarnedLoyaltyPoints(order.TotalAmount);

                var loyaltyResult = await _loyaltyService.AwardPointsForOrderAsync(userId, order.Id, order.TotalAmount);
                if (!loyaltyResult.Success)
                {
                    return new ErrorDataResult<PaymentDto>(loyaltyResult.Message);
                }

                var referralResult = await _referralService.AwardFirstPurchaseRewardsAsync(order.Id);
                if (!referralResult.Success)
                {
                    return new ErrorDataResult<PaymentDto>(referralResult.Message);
                }

                if (request.SaveCard && !request.SavedCardId.HasValue)
                {
                    await TryPersistTokenizedCardAsync(userId, request, iyzicoPayment);
                }
            }
            else
            {
                order.Payment.Status = PaymentStatus.Failed;
                order.Payment.ErrorMessage = iyzicoPayment.ErrorMessage ?? "Ödeme işlemi başarısız oldu.";
            }

            order.Payment.IdempotencyKey = idempotencyKey;

            _orderDal.Update(order);
            await _unitOfWork.SaveChangesAsync();

            if (order.Payment.Status == PaymentStatus.Success)
            {
                return new SuccessDataResult<PaymentDto>(MapToDto(order.Payment));
            }

            LogPaymentFailure(order, userId, request, requiresThreeDS, "Charge", order.Payment.ErrorMessage);
            
            return new ErrorDataResult<PaymentDto>(
                MapToDto(order.Payment), 
                order.Payment.ErrorMessage ?? Constants.InfrastructureConstants.Payment.DefaultErrorMessage,
                order.Payment.Status == PaymentStatus.Failed ? Constants.InfrastructureConstants.Payment.DefaultErrorCode : null,
                iyzicoPayment.ErrorMessage);
        }
        finally
        {
            await _lockService.ReleaseLockAsync(lockKey, lockToken);
        }
    }

    private async Task<IyzicoPaymentExecutionResult> ProcessIyzicoPaymentAsync(Order order, ProcessPaymentRequest request, bool requiresThreeDS)
    {
        var paymentRequest = new CreatePaymentRequest
        {
            Locale = Locale.TR.ToString(),
            ConversationId = order.OrderNumber,
            Price = order.TotalAmount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
            PaidPrice = order.TotalAmount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
            Currency = Currency.TRY.ToString(),
            Installment = 1,
            BasketId = $"B{order.Id}",
            PaymentChannel = PaymentChannel.WEB.ToString(),
            PaymentGroup = PaymentGroup.PRODUCT.ToString(),
            CallbackUrl = requiresThreeDS ? BuildThreeDSCallbackUrl() : null
        };

        // Kart bilgileri - kayıtlı kart veya yeni kart
        string cardHolderName, cardNumber, expireMonth, expireYear;
        
            if (request.SavedCardId.HasValue)
        {
            // Kayıtlı kart kullan
            var savedCardResult = await _creditCardService.GetStoredCardForPaymentAsync(order.UserId, request.SavedCardId.Value);
            if (!savedCardResult.Success || savedCardResult.Data == null)
            {
                throw new InvalidOperationException("Kayıtlı kart bulunamadı veya yetkisiz erişim.");
            }
            
            var savedCard = savedCardResult.Data;
            if (savedCard.IsTokenized && savedCard.TokenProvider != PaymentProviderType.Iyzico)
            {
                throw new InvalidOperationException(
                    $"Secilen kayitli kart {savedCard.TokenProvider} saglayicisi ile kayitli. Bu odeme yalnizca Iyzico ile alinabilir.");
            }

            if (savedCard.IsTokenized &&
                savedCard.TokenProvider == PaymentProviderType.Iyzico &&
                !string.IsNullOrWhiteSpace(savedCard.IyzicoCardToken) &&
                !string.IsNullOrWhiteSpace(savedCard.IyzicoUserKey))
            {
                paymentRequest.PaymentCard = new PaymentCard
                {
                    CardToken = savedCard.IyzicoCardToken,
                    CardUserKey = savedCard.IyzicoUserKey,
                    RegisterCard = 0
                };

                return await ExecuteIyzicoChargeAsync(paymentRequest, order, requiresThreeDS, savedCard.Last4Digits);
            }

            throw new InvalidOperationException(
                "Bu kayitli kart eski sifreli formatta. Guvenlik nedeniyle yeniden kart bilgisi girmeniz gerekiyor.");
        }
        else
        {
            // Yeni kart bilgileri
            cardHolderName = request.CardHolderName ?? "Test User";
            cardNumber = request.CardNumber?.Replace(" ", "") ?? "";
            expireMonth = GetExpireMonth(request.ExpiryDate);
            expireYear = GetExpireYear(request.ExpiryDate);
        }
        
        paymentRequest.PaymentCard = new PaymentCard
        {
            CardHolderName = cardHolderName,
            CardNumber = cardNumber,
            ExpireMonth = expireMonth,
            ExpireYear = expireYear,
            Cvc = request.CVV ?? "",
            RegisterCard = request.SaveCard && !request.SavedCardId.HasValue ? 1 : 0
        };
        return await ExecuteIyzicoChargeAsync(paymentRequest, order, requiresThreeDS, ResolveLast4Digits(cardNumber));
    }

    private async Task<IyzicoPaymentExecutionResult> ExecuteIyzicoChargeAsync(
        CreatePaymentRequest paymentRequest,
        Order order,
        bool requiresThreeDS,
        string? last4Digits)
    {
        // Alıcı bilgileri
        paymentRequest.Buyer = new Buyer
        {
            Id = order.UserId.ToString(),
            Name = order.User?.FirstName ?? "Test",
            Surname = order.User?.LastName ?? "User",
            GsmNumber = "+905350000000",
            Email = order.User?.Email ?? "test@test.com",
            IdentityNumber = "11111111111",
            RegistrationAddress = order.ShippingAddress,
            Ip = "85.34.78.112",
            City = "Istanbul",
            Country = "Turkey"
        };

        // Adres bilgileri
        var address = new Address
        {
            ContactName = $"{order.User?.FirstName} {order.User?.LastName}".Trim(),
            City = "Istanbul",
            Country = "Turkey",
            Description = order.ShippingAddress
        };
        paymentRequest.ShippingAddress = address;
        paymentRequest.BillingAddress = address;

        // Sepet ürünleri - Iyzico kuralı: BasketItems toplamı = PaidPrice
        var orderItemsList = order.OrderItems.ToList();
        var subtotal = orderItemsList.Sum(i => i.PriceSnapshot * i.Quantity);
        var paidPrice = order.TotalAmount;
        
        var basketItems = new List<BasketItem>();
        decimal accumulatedAdjustedTotal = 0;

        for (int i = 0; i < orderItemsList.Count; i++)
        {
            var item = orderItemsList[i];
            var itemTotal = item.PriceSnapshot * item.Quantity;
            
            decimal adjustedPrice;
            
            if (subtotal > 0)
            {
                adjustedPrice = (itemTotal / subtotal) * paidPrice;
            }
            else
            {
                adjustedPrice = 0;
            }

            // Son eleman kontrolü (Kuruş farkını düzelt)
            if (i == orderItemsList.Count - 1)
            {
                adjustedPrice = paidPrice - accumulatedAdjustedTotal;
            }
            else
            {
                adjustedPrice = Math.Round(adjustedPrice, 2);
                accumulatedAdjustedTotal += adjustedPrice;
            }
            
            if (adjustedPrice < 0) adjustedPrice = 0;

            basketItems.Add(new BasketItem
            {
                Id = $"BI{item.ProductId}",
                Name = item.Product?.Name ?? $"Product {item.ProductId}",
                Category1 = item.Product?.Category?.Name ?? "General",
                ItemType = BasketItemType.PHYSICAL.ToString(),
                Price = adjustedPrice.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
            });
        }
        
        paymentRequest.BasketItems = basketItems;

        if (requiresThreeDS)
        {
            return new IyzicoPaymentExecutionResult
            {
                ThreeDSInitialize = await _paymentGateway.InitializeThreeDSAsync(paymentRequest),
                Last4Digits = last4Digits
            };
        }

        return new IyzicoPaymentExecutionResult
        {
            Payment = await _paymentGateway.ChargeAsync(paymentRequest),
            Last4Digits = last4Digits
        };
    }

    private static string? ValidatePaymentRequest(ProcessPaymentRequest request)
    {
        if (request.SavedCardId.HasValue)
        {
            return null;
        }

        if (!IsValidCvv(request.CVV))
        {
            return "Guvenlik kodu (CVV) 3 veya 4 haneli olmalidir.";
        }

        if (string.IsNullOrWhiteSpace(request.CardHolderName) ||
            string.IsNullOrWhiteSpace(request.CardNumber) ||
            string.IsNullOrWhiteSpace(request.ExpiryDate))
        {
            return "Yeni kart ile odeme icin kart bilgileri eksiksiz girilmelidir.";
        }

        var digitsOnly = new string(request.CardNumber.Where(char.IsDigit).ToArray());
        if (digitsOnly.Length < 13 || digitsOnly.Length > 19)
        {
            return "Kart numarasi gecersizdir.";
        }

        if (!request.ExpiryDate.Contains('/'))
        {
            return "Son kullanma tarihi MM/YY veya MM/YYYY formatinda olmali.";
        }

        return null;
    }

    private async Task TryPersistTokenizedCardAsync(int userId, ProcessPaymentRequest request, IyzicoChargeGatewayResult iyzicoPayment)
    {
        if (string.IsNullOrWhiteSpace(iyzicoPayment.CardToken) ||
            string.IsNullOrWhiteSpace(iyzicoPayment.CardUserKey) ||
            string.IsNullOrWhiteSpace(iyzicoPayment.LastFourDigits))
        {
            _logger.LogWarning(
                "Save card requested but provider token metadata was missing. UserId={UserId}, OrderId={OrderId}, PaymentId={PaymentId}, CorrelationId={CorrelationId}",
                userId,
                request.OrderId,
                iyzicoPayment.PaymentId,
                _correlationIdProvider.GetCorrelationId());
            return;
        }

        var saveResult = await _creditCardService.SaveTokenizedCardAsync(userId, new SaveTokenizedCreditCardRequest
        {
            CardAlias = string.IsNullOrWhiteSpace(request.SaveCardAlias)
                ? (string.IsNullOrWhiteSpace(request.CardHolderName) ? "Kayitli Kartim" : request.CardHolderName)
                : request.SaveCardAlias,
            CardHolderName = request.CardHolderName ?? "Card Holder",
            Brand = CardBrandDetector.Detect(request.CardNumber),
            Last4Digits = iyzicoPayment.LastFourDigits,
            ExpireMonth = GetExpireMonth(request.ExpiryDate),
            ExpireYear = GetExpireYear(request.ExpiryDate),
            TokenProvider = PaymentProviderType.Iyzico,
            IyzicoCardToken = iyzicoPayment.CardToken,
            IyzicoUserKey = iyzicoPayment.CardUserKey,
            IsDefault = false
        });

        if (!saveResult.Success)
        {
            var sanitizedReason = SensitiveDataLogSanitizer.Sanitize(saveResult.Message);
            _logger.LogWarning(
                "Provider token received but local card save failed. UserId={UserId}, OrderId={OrderId}, PaymentId={PaymentId}, Reason={Reason}, CorrelationId={CorrelationId}",
                userId,
                request.OrderId,
                iyzicoPayment.PaymentId,
                sanitizedReason,
                _correlationIdProvider.GetCorrelationId());
        }
    }

    private static bool IsValidCvv(string? cvv)
    {
        if (string.IsNullOrWhiteSpace(cvv))
        {
            return false;
        }

        var digitsOnly = new string(cvv.Where(char.IsDigit).ToArray());
        return digitsOnly.Length is >= 3 and <= 4;
    }

    private static string GetExpireMonth(string? expiryDate)
    {
        if (string.IsNullOrEmpty(expiryDate) || !expiryDate.Contains('/'))
            return "12";
        return expiryDate.Split('/')[0];
    }

    private static string GetExpireYear(string? expiryDate)
    {
        if (string.IsNullOrEmpty(expiryDate) || !expiryDate.Contains('/'))
            return "2030";
        var year = expiryDate.Split('/')[1];
        return year.Length == 2 ? $"20{year}" : year;
    }

    private static string? ResolveLast4Digits(string? cardNumber)
    {
        if (string.IsNullOrWhiteSpace(cardNumber))
        {
            return null;
        }

        var digitsOnly = new string(cardNumber.Where(char.IsDigit).ToArray());
        return digitsOnly.Length >= 4 ? digitsOnly[^4..] : null;
    }

    private void LogPaymentFailure(
        Order order,
        int userId,
        ProcessPaymentRequest request,
        bool requiresThreeDS,
        string failureStage,
        string? failureReason)
    {
        _logger.LogWarning(
            "Payment analytics event. AnalyticsStream={AnalyticsStream}, AnalyticsEvent={AnalyticsEvent}, Provider={Provider}, OrderId={OrderId}, UserId={UserId}, PaymentRecordId={PaymentRecordId}, PaymentStatus={PaymentStatus}, FailureStage={FailureStage}, ErrorCode={ErrorCode}, FailureReason={FailureReason}, UsesSavedCard={UsesSavedCard}, RequiresThreeDS={RequiresThreeDS}, CorrelationId={CorrelationId}",
            "Payment",
            "PaymentFailed",
            PaymentProviderType.Iyzico,
            order.Id,
            userId,
            order.Payment?.Id,
            order.Payment?.Status.ToString(),
            failureStage,
            Constants.InfrastructureConstants.Payment.DefaultErrorCode,
            SensitiveDataLogSanitizer.Sanitize(failureReason),
            request.SavedCardId.HasValue,
            requiresThreeDS,
            _correlationIdProvider.GetCorrelationId());
    }

    private bool ShouldRequireThreeDS(decimal orderAmount, ProcessPaymentRequest request)
    {
        return _paymentSettings.Force3DSecure
            || orderAmount >= _paymentSettings.Force3DSecureAbove
            || request.Require3DS;
    }

    private string BuildThreeDSCallbackUrl()
    {
        var baseUrl = _paymentSettings.PublicApiBaseUrl?.Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException("PublicApiBaseUrl ayari bulunamadi.");
        }

        return $"{baseUrl}/api/v1/payments/callback";
    }

    public async Task<IDataResult<PaymentDto>> GetPaymentByOrderIdAsync(int orderId)
    {
        var order = await _orderDal.GetByIdWithDetailsAsync(orderId);

        if (order?.Payment == null)
            return new ErrorDataResult<PaymentDto>("Ödeme bulunamadı");

        return new SuccessDataResult<PaymentDto>(MapToDto(order.Payment));
    }

    private static PaymentDto MapToDto(EcommerceAPI.Entities.Concrete.Payment payment)
    {
        return new PaymentDto
        {
            Id = payment.Id,
            Amount = payment.Amount,
            Currency = payment.Currency,
            Status = payment.Status.ToString(),
            PaymentMethod = payment.PaymentMethod,
            Provider = payment.Provider,
            Last4Digits = payment.Last4Digits,
            ErrorMessage = payment.ErrorMessage,
            CreatedAt = payment.CreatedAt
        };
    }

    private static int CalculateEarnedLoyaltyPoints(decimal paidAmount)
    {
        return (int)Math.Floor(Math.Max(0m, paidAmount));
    }

    #region Webhook Processing

    public async Task<IResult> ProcessWebhookAsync(IyzicoWebhookRequest request, string signatureHeader)
    {
        // Signature doğrulama
        if (!ValidateSignature(request, signatureHeader))
        {
            return new ErrorResult(
                "Invalid signature",
                Constants.InfrastructureConstants.Payment.WebhookInvalidSignatureCode);
        }

        // ConversationId ile siparişi bul
        if (string.IsNullOrEmpty(request.PaymentConversationId))
             return new ErrorResult(
                 "ConversationId is missing",
                 Constants.InfrastructureConstants.Payment.WebhookConversationIdMissingCode);

        var order = await _orderDal.GetByOrderNumberAsync(request.PaymentConversationId);
        if (order == null)
             return new ErrorResult(
                 "Order not found",
                 Constants.InfrastructureConstants.Payment.OrderNotFoundCode);

        // Idempotency: Zaten işlenmiş mi?
        if (order.Status == OrderStatus.Paid)
            return new SuccessResult("Already paid");

        // Direct webhook - status'a göre güncelle
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            if (order.Payment == null)
            {
                 await _unitOfWork.RollbackTransactionAsync();
                 return new ErrorResult(
                     "Payment record not found on order",
                     Constants.InfrastructureConstants.Payment.PaymentRecordNotFoundCode);
            }

            if (request.Status?.ToUpperInvariant() == "SUCCESS")
            {
                order.Payment.Status = PaymentStatus.Success;
                order.Payment.PaymentProviderId = request.IyziPaymentId ?? request.PaymentId;
                order.Status = OrderStatus.Paid;
                order.LoyaltyPointsEarned = CalculateEarnedLoyaltyPoints(order.TotalAmount);

                var loyaltyResult = await _loyaltyService.AwardPointsForOrderAsync(order.UserId, order.Id, order.TotalAmount);
                if (!loyaltyResult.Success)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return new ErrorResult(loyaltyResult.Message);
                }
            }
            else
            {
                order.Payment.Status = PaymentStatus.Failed;
                order.Payment.ErrorMessage = "Webhook: Ödeme başarısız";
            }

            _orderDal.Update(order);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            return new SuccessResult("Webhook processed");
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<IResult> VerifyAndFinalizePaymentAsync(string paymentId, string conversationId, string conversationData)
    {
        var options = new Iyzipay.Options
        {
            ApiKey = _settings.ApiKey,
            SecretKey = _settings.SecretKey,
            BaseUrl = _settings.BaseUrl
        };

        var threeDSPaymentRequest = new CreateThreedsPaymentRequest
        {
            ConversationId = conversationId,
            PaymentId = paymentId,
            ConversationData = conversationData,
            Locale = Locale.TR.ToString()
        };

        var threeDSPayment = await ThreedsPayment.Create(threeDSPaymentRequest, options);

        var order = await _orderDal.GetByOrderNumberAsync(conversationId);
        if (order == null || order.Payment == null)
            return new ErrorResult("Order or payment not found");

        // idempotency check
        if (order.Status == OrderStatus.Paid)
            return new SuccessResult("Already paid");

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            if (threeDSPayment.Status == "success" && threeDSPayment.PaymentStatus == "SUCCESS")
            {
                order.Payment.Status = PaymentStatus.Success;
                order.Payment.Provider = PaymentProviderType.Iyzico;
                order.Payment.PaymentProviderId = threeDSPayment.PaymentId;
                order.Payment.ErrorMessage = null;
                order.Status = OrderStatus.Paid;
                order.LoyaltyPointsEarned = CalculateEarnedLoyaltyPoints(order.TotalAmount);

                var loyaltyResult = await _loyaltyService.AwardPointsForOrderAsync(order.UserId, order.Id, order.TotalAmount);
                if (!loyaltyResult.Success)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return new ErrorResult(loyaltyResult.Message);
                }

                var referralResult = await _referralService.AwardFirstPurchaseRewardsAsync(order.Id);
                if (!referralResult.Success)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return new ErrorResult(referralResult.Message);
                }
            }
            else
            {
                order.Payment.Status = PaymentStatus.Failed;
                order.Payment.ErrorMessage = threeDSPayment.ErrorMessage ?? "3D Secure odeme dogrulamasi basarisiz";
            }

            _orderDal.Update(order);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            return threeDSPayment.Status == "success" 
                ? new SuccessResult(@"Payment verified and updated") 
                : new ErrorResult(threeDSPayment.ErrorMessage ?? "Payment verification failed");
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    /// <summary>
    /// iyzico X-IYZ-SIGNATURE-V3 doğrulaması.
    /// Bypass davranışı yalnızca config ile açılabilir (varsayılan kapalı).
    /// </summary>
    private bool ValidateSignature(IyzicoWebhookRequest request, string signatureHeader)
    {
        if (_paymentSettings.AllowWebhookSignatureBypass)
        {
            _logger.LogWarning("Webhook signature validation bypass is enabled via configuration.");
            return true;
        }

        var normalizedSignatureHeader = signatureHeader?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(normalizedSignatureHeader))
        {
            return !_paymentSettings.RequireWebhookSignature;
        }

        if (string.IsNullOrWhiteSpace(_settings.SecretKey))
        {
            _logger.LogWarning("Webhook signature validation failed because Iyzico SecretKey is missing.");
            return false;
        }

        var dataToSign = BuildSignaturePayload(request);
        var computedSignature = ComputeHmacSha256Hex(dataToSign);

        return string.Equals(computedSignature, normalizedSignatureHeader, StringComparison.OrdinalIgnoreCase);
    }

    private string BuildSignaturePayload(IyzicoWebhookRequest request)
    {
        return $"{_settings.SecretKey}{request.IyziEventType}{request.PaymentId}{request.PaymentConversationId}{request.Status}";
    }

    /// <summary>
    /// HMAC-SHA256 hesaplayıp HEX olarak döndür
    /// </summary>
    private string ComputeHmacSha256Hex(string data)
    {
        using var hmac = new System.Security.Cryptography.HMACSHA256(
            System.Text.Encoding.UTF8.GetBytes(_settings.SecretKey));
        var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data));
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    #endregion
}
