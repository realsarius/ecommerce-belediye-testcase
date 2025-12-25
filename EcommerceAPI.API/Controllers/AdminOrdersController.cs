using EcommerceAPI.Business.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EcommerceAPI.Entities.DTOs;
using System.Security.Claims;

namespace EcommerceAPI.API.Controllers;

[Authorize(Roles = "Admin,Seller")]
[ApiController]
[Route("api/v1/admin/orders")]
public class AdminOrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ISellerProfileService _sellerProfileService;

    public AdminOrdersController(IOrderService orderService, ISellerProfileService sellerProfileService)
    {
        _orderService = orderService;
        _sellerProfileService = sellerProfileService;
    }

    private (int? UserId, string? Role) GetCurrentUser()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        var roleClaim = User.FindFirst(ClaimTypes.Role);
        
        int? userId = null;
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var id))
            userId = id;
            
        return (userId, roleClaim?.Value);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllOrders()
    {
        var (userId, role) = GetCurrentUser();

        if (role == "Seller" && userId.HasValue)
        {
            var profileResult = await _sellerProfileService.GetByUserIdAsync(userId.Value);
            if (!profileResult.Success || profileResult.Data == null)
                return Ok(new List<OrderDto>());

            var result = await _orderService.GetOrdersForSellerAsync(profileResult.Data.Id);
            if (result.Success) return Ok(result.Data);
            return BadRequest(result);
        }

        var adminResult = await _orderService.GetAllOrdersAsync();
        if (adminResult.Success)
        {
            return Ok(adminResult.Data);
        }
        return BadRequest(adminResult);
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusRequest request)
    {
        var (userId, role) = GetCurrentUser();
        int? sellerId = null;

        if (role == "Seller" && userId.HasValue)
        {
            var profileResult = await _sellerProfileService.GetByUserIdAsync(userId.Value);
            if (!profileResult.Success || profileResult.Data == null)
                return BadRequest("Satıcı profili bulunamadı.");
            sellerId = profileResult.Data.Id;
        }

        var result = await _orderService.UpdateOrderStatusAsync(id, request.Status, sellerId);
        
        if (result.Success)
        {
            return Ok(result.Data);
        }
        return BadRequest(result);
    }
}
