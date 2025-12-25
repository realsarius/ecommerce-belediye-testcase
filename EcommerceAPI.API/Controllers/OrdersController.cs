using System.Security.Claims;
using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceAPI.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    private int GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var userId))
            throw new UnauthorizedAccessException("Geçersiz kullanıcı kimliği");
        return userId;
    }

    [HttpPost]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
    {

        var userId = GetUserId();
        var result = await _orderService.CheckoutAsync(userId, request);
        
        if (result.Success)
        {
            return CreatedAtAction(nameof(GetOrder), new { id = result.Data.Id }, result);
        }
        return BadRequest(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders()
    {
        var userId = GetUserId();
        var result = await _orderService.GetUserOrdersAsync(userId);
        
        if (result.Success)
        {
            return Ok(result);
        }
        return BadRequest(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(int id)
    {
        var userId = GetUserId();
        var result = await _orderService.GetOrderAsync(userId, id);
        if (result.Success)
        {
            return Ok(result);
        }
        return BadRequest(result);
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> CancelOrder(int id, [FromBody] UpdateOrderStatusRequest request)
    {
        var userId = GetUserId();

        var result = await _orderService.CancelOrderAsync(userId, id, request.Status);
        if (result.Success)
        {
            return Ok(result);
        }
        return BadRequest(result);
    }

    [HttpPut("{id}/items")]
    public async Task<IActionResult> UpdateOrderItems(int id, [FromBody] UpdateOrderItemsRequest request)
    {
        var userId = GetUserId();
        var result = await _orderService.UpdateOrderItemsAsync(userId, id, request);
        if (result.Success)
        {
            return Ok(result);
        }
        return BadRequest(result);
    }
}


