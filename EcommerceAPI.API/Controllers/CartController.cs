using System.Security.Claims;
using EcommerceAPI.Business.Services.Abstract;
using EcommerceAPI.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceAPI.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class CartController : ControllerBase
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
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CartDto>> GetCart()
    {
        var userId = GetUserId();
        var cart = await _cartService.GetCartAsync(userId);
        return Ok(cart);
    }

    [HttpPost("items")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartDto>> AddToCart([FromBody] AddToCartRequest request)
    {
        var userId = GetUserId();
        var cart = await _cartService.AddToCartAsync(userId, request);
        return Ok(cart);
    }

    [HttpPut("items/{productId}")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartDto>> UpdateCartItem(int productId, [FromBody] UpdateCartItemRequest request)
    {
        var userId = GetUserId();
        var cart = await _cartService.UpdateCartItemAsync(userId, productId, request);
        return Ok(cart);
    }

    [HttpDelete("items/{productId}")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartDto>> RemoveFromCart(int productId)
    {
        var userId = GetUserId();
        var cart = await _cartService.RemoveFromCartAsync(userId, productId);
        return Ok(cart);
    }

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ClearCart()
    {
        var userId = GetUserId();
        var result = await _cartService.ClearCartAsync(userId);
        
        if (!result)
            return NotFound(new { message = "Sepet bulunamadı" });
        
        return NoContent();
    }
}
