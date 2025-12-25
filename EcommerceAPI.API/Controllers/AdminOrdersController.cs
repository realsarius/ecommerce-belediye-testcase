using EcommerceAPI.Business.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EcommerceAPI.Entities.DTOs;

namespace EcommerceAPI.API.Controllers;

[Authorize(Roles = "Admin,Seller")]
[ApiController]
[Route("api/v1/admin/orders")]
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
        var result = await _orderService.GetAllOrdersAsync();
        if (result.Success)
        {
            return Ok(result.Data);
        }
        return BadRequest(result);
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusRequest request)
    {
        var result = await _orderService.UpdateOrderStatusAsync(id, request.Status);
        
        if (result.Success)
        {
            return Ok(result.Data);
        }
        return BadRequest(result);
    }
}

