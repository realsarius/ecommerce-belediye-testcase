using EcommerceAPI.Application.Abstractions.ServiceContracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EcommerceAPI.Entities.DTOs;

namespace EcommerceAPI.API.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/v1/admin/orders")]
public class AdminOrdersController : BaseApiController
{
    private readonly IOrderService _orderService;

    public AdminOrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllOrders([FromQuery] string? status = null, [FromQuery] decimal? minAmount = null, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        var result = await _orderService.GetAllOrdersAsync(status, minAmount, from, to);
        return HandleResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(int id)
    {
        var result = await _orderService.GetAdminOrderAsync(id);
        return HandleResult(result);
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusRequest request)
    {
        var result = await _orderService.UpdateOrderStatusAsync(id, request.Status);
        return HandleResult(result);
    }
}
