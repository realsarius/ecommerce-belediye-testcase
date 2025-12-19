using EcommerceAPI.Core.DTOs;

namespace EcommerceAPI.Business.Services.Abstract;

public interface IOrderService
{
    Task<OrderDto> CheckoutAsync(int userId, CheckoutRequest request);
    Task<OrderDto> GetOrderAsync(int userId, int orderId);
    Task<List<OrderDto>> GetUserOrdersAsync(int userId);
    Task<OrderDto> CancelOrderAsync(int userId, int orderId);
    Task CancelExpiredOrdersAsync();
}
