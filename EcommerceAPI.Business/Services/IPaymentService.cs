using EcommerceAPI.Core.DTOs;

namespace EcommerceAPI.Business.Services;

public interface IPaymentService
{
    Task<PaymentDto> ProcessPaymentAsync(int userId, ProcessPaymentRequest request);
    Task<PaymentDto?> GetPaymentByOrderIdAsync(int orderId);
}
