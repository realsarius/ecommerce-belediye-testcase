using EcommerceAPI.Business.Services.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceAPI.API.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/v1/[controller]")]
public class AdminOrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public AdminOrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllOrders()
    {
        var orders = await _orderService.GetAllOrdersAsync();
        return Ok(orders);
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] string status)
    {
        var order = await _orderService.UpdateOrderStatusAsync(id, status);
        return Ok(order);
    }
}
