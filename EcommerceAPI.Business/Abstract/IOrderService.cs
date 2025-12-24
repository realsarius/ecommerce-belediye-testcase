using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Core.Utilities.Results;

namespace EcommerceAPI.Business.Abstract;

public interface IOrderService
{
    Task<IDataResult<OrderDto>> CheckoutAsync(int userId, CheckoutRequest request);
    Task<IDataResult<OrderDto>> GetOrderAsync(int userId, int orderId);
    Task<IDataResult<List<OrderDto>>> GetUserOrdersAsync(int userId);
    Task<IDataResult<OrderDto>> CancelOrderAsync(int userId, int orderId);
    Task<IResult> CancelExpiredOrdersAsync();

    Task<IDataResult<List<OrderDto>>> GetAllOrdersAsync();
    Task<IDataResult<OrderDto>> UpdateOrderStatusAsync(int orderId, string status);
}

