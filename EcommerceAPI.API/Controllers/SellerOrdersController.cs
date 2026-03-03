using System.Security.Claims;
using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceAPI.API.Controllers;

[ApiController]
[Route("api/v1/seller/orders")]
[Authorize(Roles = "Seller")]
public class SellerOrdersController : BaseApiController
{
    private readonly IOrderService _orderService;
    private readonly ISellerProfileService _sellerProfileService;

    public SellerOrdersController(IOrderService orderService, ISellerProfileService sellerProfileService)
    {
        _orderService = orderService;
        _sellerProfileService = sellerProfileService;
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders()
    {
        var sellerId = await ResolveSellerIdAsync();
        if (sellerId == null)
        {
            return BadRequest(new { success = false, message = "Satıcı profili bulunamadı." });
        }

        var result = await _orderService.GetOrdersForSellerAsync(sellerId.Value);
        return HandleResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(int id)
    {
        var sellerId = await ResolveSellerIdAsync();
        if (sellerId == null)
        {
            return BadRequest(new { success = false, message = "Satıcı profili bulunamadı." });
        }

        var result = await _orderService.GetSellerOrderAsync(sellerId.Value, id);
        return HandleResult(result);
    }

    [HttpPut("{id}/ship")]
    public async Task<IActionResult> ShipOrder(int id, [FromBody] ShipOrderRequest request)
    {
        var sellerId = await ResolveSellerIdAsync();
        if (sellerId == null)
        {
            return BadRequest(new { success = false, message = "Satıcı profili bulunamadı." });
        }

        var result = await _orderService.ShipOrderAsync(sellerId.Value, id, request);
        return HandleResult(result);
    }

    private async Task<int?> ResolveSellerIdAsync()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            return null;
        }

        var profileResult = await _sellerProfileService.GetByUserIdAsync(userId);
        return profileResult.Success ? profileResult.Data?.Id : null;
    }
}
