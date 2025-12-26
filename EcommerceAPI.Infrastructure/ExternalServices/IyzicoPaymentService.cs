using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Infrastructure.Settings;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.Enums;
using Iyzipay;
using Iyzipay.Model;
using Iyzipay.Request;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EcommerceAPI.Infrastructure.ExternalServices;

/// <summary>
/// Iyzico ödeme API entegrasyonu.
/// </summary>
public class IyzicoPaymentService : IPaymentService
{
    private readonly IOrderDal _orderDal;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IyzicoSettings _settings;
    private readonly ICreditCardService _creditCardService;
    private readonly Microsoft.Extensions.Logging.ILogger<IyzicoPaymentService> _logger;

    public IyzicoPaymentService(
        IOrderDal orderDal,
        IUnitOfWork unitOfWork,
        IOptions<IyzicoSettings> settings,
        ICreditCardService creditCardService,
        Microsoft.Extensions.Logging.ILogger<IyzicoPaymentService> logger)
    {
        _orderDal = orderDal;
        _unitOfWork = unitOfWork;
        _settings = settings.Value;
        _creditCardService = creditCardService;
        _logger = logger;
    }

    public async Task<IDataResult<PaymentDto>> ProcessPaymentAsync(int userId, ProcessPaymentRequest request)
    {
        _logger.LogInformation("RAW PAYMENT REQUEST: User={UserId}, OrderId={OrderId}, SavedCardId={SavedCardId}, CardNoLen={CardLen}", 
            userId, request.OrderId, request.SavedCardId, request.CardNumber?.Length ?? 0);

        var order = await _orderDal.GetByIdWithDetailsAsync(request.OrderId);

        if (order == null || order.UserId != userId)
            return new ErrorDataResult<PaymentDto>(
                Constants.InfrastructureConstants.Payment.OrderNotFoundCode, 
                $"Sipariş bulunamadı: {request.OrderId}");

        if (order.Payment == null)
            return new ErrorDataResult<PaymentDto>(
                Constants.InfrastructureConstants.Payment.PaymentRecordNotFoundCode,
                "Bu siparişe ait ödeme kaydı bulunamadı.");

        // Generate idempotency key if not provided
        var idempotencyKey = string.IsNullOrEmpty(request.IdempotencyKey) 
            ? $"{order.OrderNumber}-{userId}-{DateTime.UtcNow:yyyyMMddHHmmss}" 
            : request.IdempotencyKey;

        // Idempotency kontrolü
        if (order.Payment.IdempotencyKey == idempotencyKey &&
            order.Payment.Status == PaymentStatus.Success)
        {
            return new SuccessDataResult<PaymentDto>(MapToDto(order.Payment));
        }

        if (order.Payment.Status == PaymentStatus.Success)
            return new ErrorDataResult<PaymentDto>(
                Constants.InfrastructureConstants.Payment.AlreadyPaidCode,
                "Bu sipariş için ödeme zaten alınmış.");

        // Iyzico API isteği
        var iyzicoPayment = await ProcessIyzicoPaymentAsync(order, request);

        // Sonucu işle
        if (iyzicoPayment.Status == "success")
        {
            order.Payment.Status = PaymentStatus.Success;
            order.Payment.PaymentProviderId = iyzicoPayment.PaymentId;
            order.Status = OrderStatus.Paid;
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
        
        return new ErrorDataResult<PaymentDto>(
            MapToDto(order.Payment), 
            order.Payment.ErrorMessage ?? Constants.InfrastructureConstants.Payment.DefaultErrorMessage,
            order.Payment.Status == PaymentStatus.Failed ? Constants.InfrastructureConstants.Payment.DefaultErrorCode : null,
            iyzicoPayment.ErrorMessage);
    }

    private async Task<Iyzipay.Model.Payment> ProcessIyzicoPaymentAsync(Order order, ProcessPaymentRequest request)
    {
        var options = new Iyzipay.Options
        {
            ApiKey = _settings.ApiKey,
            SecretKey = _settings.SecretKey,
            BaseUrl = _settings.BaseUrl
        };

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
            PaymentGroup = PaymentGroup.PRODUCT.ToString()
        };

        // Kart bilgileri - kayıtlı kart veya yeni kart
        string cardHolderName, cardNumber, expireMonth, expireYear;
        
        if (request.SavedCardId.HasValue)
        {
            // Kayıtlı kart kullan
            var savedCardResult = await _creditCardService.GetDecryptedCardForPaymentAsync(order.UserId, request.SavedCardId.Value);
            if (!savedCardResult.Success || savedCardResult.Data == null)
            {
                throw new InvalidOperationException("Kayıtlı kart bulunamadı veya yetkisiz erişim.");
            }
            
            var savedCard = savedCardResult.Data;
            cardHolderName = savedCard.CardHolderName;
            cardNumber = savedCard.CardNumber?.Replace(" ", "") ?? "";
            expireMonth = savedCard.ExpireMonth;
            expireYear = savedCard.ExpireYear;

            _logger.LogInformation("Kayıtlı kart kullanılıyor ID: {SavedCardId}", request.SavedCardId);
            _logger.LogInformation("Kart No Length: {Length}, IsNumeric: {IsNumeric}", 
                cardNumber.Length, 
                cardNumber.All(char.IsDigit));
            _logger.LogInformation("Expire: {Month}/{Year}", expireMonth, expireYear);

            if (!cardNumber.All(char.IsDigit))
            {
                _logger.LogError("DECRYPTION HATASI: Kart numarası sadece rakamlardan oluşmuyor! Decrypted: {Decrypted}", 
                    cardNumber.Length > 4 ? cardNumber.Substring(0, 4) + "***" : "INVALID");
            }
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
            RegisterCard = 0
        };

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

        return await Iyzipay.Model.Payment.Create(paymentRequest, options);
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
            ErrorMessage = payment.ErrorMessage,
            CreatedAt = payment.CreatedAt
        };
    }

    #region Webhook Processing

    public async Task<IResult> ProcessWebhookAsync(IyzicoWebhookRequest request, string signatureHeader)
    {
        // Signature doğrulama
        if (!ValidateSignature(request, signatureHeader))
        {
            return new ErrorResult("Invalid signature");
        }

        // ConversationId ile siparişi bul
        if (string.IsNullOrEmpty(request.PaymentConversationId))
             return new ErrorResult("ConversationId is missing");

        var order = await _orderDal.GetByOrderNumberAsync(request.PaymentConversationId);
        if (order == null)
             return new ErrorResult("Order not found");

        // Idempotency: Zaten işlenmiş mi?
        if (order.Status == OrderStatus.Paid)
            return new SuccessResult("Already paid");

        // Token varsa - Double Verification
        if (!string.IsNullOrEmpty(request.Token))
        {
            return await VerifyAndFinalizePaymentAsync(request.Token, request.PaymentConversationId);
        }

        // Direct webhook - status'a göre güncelle
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            if (order.Payment == null)
            {
                 await _unitOfWork.RollbackTransactionAsync();
                 return new ErrorResult("Payment record not found on order");
            }

            if (request.Status?.ToUpperInvariant() == "SUCCESS")
            {
                order.Payment.Status = PaymentStatus.Success;
                order.Payment.PaymentProviderId = request.IyziPaymentId ?? request.PaymentId;
                order.Status = OrderStatus.Paid;
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

    public async Task<IResult> VerifyAndFinalizePaymentAsync(string token, string conversationId)
    {
        var options = new Iyzipay.Options
        {
            ApiKey = _settings.ApiKey,
            SecretKey = _settings.SecretKey,
            BaseUrl = _settings.BaseUrl
        };

        var retrieveRequest = new RetrieveCheckoutFormRequest
        {
            ConversationId = conversationId,
            Token = token
        };

        // iyzico'dan gerçek sonucu al
        var checkoutForm = await CheckoutForm.Retrieve(retrieveRequest, options);

        var order = await _orderDal.GetByOrderNumberAsync(conversationId);
        if (order == null || order.Payment == null)
            return new ErrorResult("Order or payment not found");

        // idempotency check
        if (order.Status == OrderStatus.Paid)
            return new SuccessResult("Already paid");

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            if (checkoutForm.Status == "success" && checkoutForm.PaymentStatus == "SUCCESS")
            {
                order.Payment.Status = PaymentStatus.Success;
                order.Payment.PaymentProviderId = checkoutForm.PaymentId;
                order.Status = OrderStatus.Paid;
            }
            else
            {
                order.Payment.Status = PaymentStatus.Failed;
                order.Payment.ErrorMessage = checkoutForm.ErrorMessage ?? "Ödeme doğrulaması başarısız";
            }

            _orderDal.Update(order);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            return checkoutForm.Status == "success" 
                ? new SuccessResult(@"Payment verified and updated") 
                : new ErrorResult(checkoutForm.ErrorMessage ?? "Payment verification failed");
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    /// <summary>
    /// iyzico X-IYZ-SIGNATURE-V3 doğrulaması
    /// Format: SECRET KEY + iyziEventType + paymentId + paymentConversationId + status
    /// Development: Boş signature kabul edilir (test için)
    /// </summary>
    private bool ValidateSignature(IyzicoWebhookRequest request, string signatureHeader)
    {
        // Development mode: Signature yoksa bypass et
        if (string.IsNullOrEmpty(signatureHeader))
        {
            var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
            return isDevelopment;
        }

        var dataToSign = $"{_settings.SecretKey}{request.IyziEventType}{request.PaymentId}{request.PaymentConversationId}{request.Status}";
        var computedSignature = ComputeHmacSha256Hex(dataToSign);

        return string.Equals(computedSignature, signatureHeader, StringComparison.OrdinalIgnoreCase);
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
