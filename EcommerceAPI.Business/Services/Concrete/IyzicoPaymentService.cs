using EcommerceAPI.Business.Services.Abstract;
using EcommerceAPI.Business.Settings;
using EcommerceAPI.Core.DTOs;
using EcommerceAPI.Core.Entities;
using EcommerceAPI.Core.Enums;
using EcommerceAPI.Core.Exceptions;
using EcommerceAPI.Core.Interfaces;
using Iyzipay;
using Iyzipay.Model;
using Iyzipay.Request;
using Microsoft.Extensions.Options;

namespace EcommerceAPI.Business.Services.Concrete;

public class IyzicoPaymentService : IPaymentService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IyzicoSettings _settings;

    public IyzicoPaymentService(
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        IOptions<IyzicoSettings> settings)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _settings = settings.Value;
    }

    public async Task<PaymentDto> ProcessPaymentAsync(int userId, ProcessPaymentRequest request)
    {
        var order = await _orderRepository.GetByIdWithDetailsAsync(request.OrderId);

        if (order == null || order.UserId != userId)
            throw new NotFoundException("Sipariş", request.OrderId);

        if (order.Payment == null)
            throw new DomainException("Bu siparişe ait ödeme kaydı bulunamadı.");

        if (order.Payment.Status == PaymentStatus.Success)
            throw new DomainException("Bu sipariş için ödeme zaten alınmış.");

        if (order.Status == OrderStatus.Cancelled)
            throw new DomainException("İptal edilmiş siparişler için ödeme yapılamaz.");

        // Idempotency kontrolü
        if (!string.IsNullOrEmpty(request.IdempotencyKey) &&
            order.Payment.IdempotencyKey == request.IdempotencyKey &&
            order.Payment.Status == PaymentStatus.Success)
        {
            return MapToDto(order.Payment);
        }

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

        if (!string.IsNullOrEmpty(request.IdempotencyKey))
        {
            order.Payment.IdempotencyKey = request.IdempotencyKey;
        }

        _orderRepository.Update(order);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(order.Payment);
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

        // Kart bilgileri
        paymentRequest.PaymentCard = new PaymentCard
        {
            CardHolderName = request.CardHolderName ?? "Test User",
            CardNumber = request.CardNumber?.Replace(" ", "") ?? "",
            ExpireMonth = GetExpireMonth(request.ExpiryDate),
            ExpireYear = GetExpireYear(request.ExpiryDate),
            Cvc = request.CVV ?? "",
            RegisterCard = 0
        };

        // Alıcı bilgileri
        // User'dan gelen veriler ile doldurulabilir
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

        // Sepet ürünleri
        paymentRequest.BasketItems = order.OrderItems.Select(item => new BasketItem
        {
            Id = $"BI{item.ProductId}",
            Name = item.Product?.Name ?? $"Product {item.ProductId}",
            Category1 = item.Product?.Category?.Name ?? "General",
            ItemType = BasketItemType.PHYSICAL.ToString(),
            Price = (item.PriceSnapshot * item.Quantity).ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
        }).ToList();

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

    public async Task<PaymentDto?> GetPaymentByOrderIdAsync(int orderId)
    {
        var order = await _orderRepository.GetByIdWithDetailsAsync(orderId);

        if (order?.Payment == null)
            return null;

        return MapToDto(order.Payment);
    }

    private static PaymentDto MapToDto(EcommerceAPI.Core.Entities.Payment payment)
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

    public async Task<bool> ProcessWebhookAsync(IyzicoWebhookRequest request, string signatureHeader)
    {
        // Signature doğrulama
        if (!ValidateSignature(request, signatureHeader))
        {
            return false;
        }

        // ConversationId ile siparişi bul
        if (string.IsNullOrEmpty(request.PaymentConversationId))
            return false;

        var order = await _orderRepository.GetByOrderNumberAsync(request.PaymentConversationId);
        if (order == null)
            return false;

        // Idempotency: Zaten işlenmiş mi?
        if (order.Status == OrderStatus.Paid)
            return true;

        // Token varsa - Double Verification
        if (!string.IsNullOrEmpty(request.Token))
        {
            return await VerifyAndFinalizePaymentAsync(request.Token, request.PaymentConversationId);
        }

        // Direct webhook - status'a göre güncelle
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            if (request.Status?.ToUpperInvariant() == "SUCCESS" && order.Payment != null)
            {
                order.Payment.Status = PaymentStatus.Success;
                order.Payment.PaymentProviderId = request.IyziPaymentId ?? request.PaymentId;
                order.Status = OrderStatus.Paid;
            }
            else if (order.Payment != null)
            {
                order.Payment.Status = PaymentStatus.Failed;
                order.Payment.ErrorMessage = "Webhook: Ödeme başarısız";
            }

            _orderRepository.Update(order);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            return true;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<bool> VerifyAndFinalizePaymentAsync(string token, string conversationId)
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

        var order = await _orderRepository.GetByOrderNumberAsync(conversationId);
        if (order == null || order.Payment == null)
            return false;

        // idempotency check
        if (order.Status == OrderStatus.Paid)
            return true;

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

            _orderRepository.Update(order);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            return checkoutForm.Status == "success";
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }


    // iyzico X-IYZ-SIGNATURE-V3 doğrulaması
    // Format: SECRET KEY + iyziEventType + paymentId + paymentConversationId + status
    // Development: Boş signature kabul edilir (test için)
    private bool ValidateSignature(IyzicoWebhookRequest request, string signatureHeader)
    {
        // Development mode: Signature yoksa bypass et
        if (string.IsNullOrEmpty(signatureHeader))
        {
            // Production'da false dönmeli, development'da bypass
            var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
            return isDevelopment; // Sadece Development'ta geçir
        }

        var dataToSign = $"{_settings.SecretKey}{request.IyziEventType}{request.PaymentId}{request.PaymentConversationId}{request.Status}";
        var computedSignature = ComputeHmacSha256Hex(dataToSign);

        return string.Equals(computedSignature, signatureHeader, StringComparison.OrdinalIgnoreCase);
    }


    // HMAC-SHA256 hesaplayıp HEX olarak döndür
    private string ComputeHmacSha256Hex(string data)
    {
        using var hmac = new System.Security.Cryptography.HMACSHA256(
            System.Text.Encoding.UTF8.GetBytes(_settings.SecretKey));
        var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data));
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    #endregion
}
