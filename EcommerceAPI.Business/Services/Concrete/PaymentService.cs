using EcommerceAPI.Business.Services.Abstract;
using EcommerceAPI.Core.DTOs;
using EcommerceAPI.Core.Entities;
using EcommerceAPI.Core.Enums;
using EcommerceAPI.Core.Exceptions;
using EcommerceAPI.Core.Interfaces;

namespace EcommerceAPI.Business.Services.Concrete;

public class PaymentService : IPaymentService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly Random _random = new();

    public PaymentService(IOrderRepository orderRepository, IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
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

        if (!string.IsNullOrEmpty(request.IdempotencyKey) && 
            order.Payment.IdempotencyKey == request.IdempotencyKey &&
            order.Payment.Status == PaymentStatus.Success)
        {
            return MapToDto(order.Payment);
        }

        var isSuccess = _random.Next(1, 11) <= 9;

        if (isSuccess)
        {
            order.Payment.Status = PaymentStatus.Success;
            order.Payment.PaymentProviderId = $"PAY-{Guid.NewGuid().ToString()[..12].ToUpper()}";
            order.Status = OrderStatus.Paid;
        }
        else
        {
            order.Payment.Status = PaymentStatus.Failed;
            order.Payment.ErrorMessage = "Ödeme işlemi başarısız oldu. Lütfen tekrar deneyin.";
        }

        if (!string.IsNullOrEmpty(request.IdempotencyKey))
        {
            order.Payment.IdempotencyKey = request.IdempotencyKey;
        }

        _orderRepository.Update(order);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(order.Payment);
    }

    public async Task<PaymentDto?> GetPaymentByOrderIdAsync(int orderId)
    {
        var order = await _orderRepository.GetByIdWithDetailsAsync(orderId);
        
        if (order?.Payment == null)
            return null;

        return MapToDto(order.Payment);
    }

    private static PaymentDto MapToDto(Payment payment)
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
}
