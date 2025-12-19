using System.Security.Claims;
using EcommerceAPI.Business.Services;
using EcommerceAPI.Core.DTOs;
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

    [HttpPost("checkout")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderDto>> Checkout([FromBody] CheckoutRequest request)
    {
        var userId = GetUserId();
        var order = await _orderService.CheckoutAsync(userId, request);
        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<OrderDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<OrderDto>>> GetOrders()
    {
        var userId = GetUserId();
        var orders = await _orderService.GetUserOrdersAsync(userId);
        return Ok(orders);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetOrder(int id)
    {
        var userId = GetUserId();
        var order = await _orderService.GetOrderAsync(userId, id);
        return Ok(order);
    }

    [HttpPost("{id}/cancel")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> CancelOrder(int id)
    {
        var userId = GetUserId();
        var order = await _orderService.CancelOrderAsync(userId, id);
        return Ok(order);
    }
}
