using System.Security.Claims;
using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceAPI.API.Controllers;

/// <summary>
/// Cart management endpoints.
/// </summary>
[Route("api/v1/[controller]")]
[Authorize]
public class CartController : BaseApiController
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    private int GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var userId))
            throw new UnauthorizedAccessException("Geçersiz kullanıcı kimliği");
        return userId;
    }

    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        var userId = GetUserId();
        var result = await _cartService.GetCartAsync(userId);
        return HandleResult(result);
    }

    [HttpPost("items")]
    public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
    {
        var userId = GetUserId();
        var result = await _cartService.AddToCartAsync(userId, request);
        return HandleResult(result);
    }

    [HttpPut("items/{productId}")]
    public async Task<IActionResult> UpdateCartItem(int productId, [FromBody] UpdateCartItemRequest request)
    {
        var userId = GetUserId();
        var result = await _cartService.UpdateCartItemAsync(userId, productId, request);
        return HandleResult(result);
    }

    [HttpDelete("items/{productId}")]
    public async Task<IActionResult> RemoveFromCart(int productId)
    {
        var userId = GetUserId();
        var result = await _cartService.RemoveFromCartAsync(userId, productId);
        return HandleResult(result);
    }

    [HttpDelete]
    public async Task<IActionResult> ClearCart()
    {
        var userId = GetUserId();
        var result = await _cartService.ClearCartAsync(userId);
        return HandleResult(result);
    }
}
