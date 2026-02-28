using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EcommerceAPI.API.Controllers;

[Route("api/v1/[controller]")]
[ApiController]
[Authorize]
public class WishlistsController : BaseApiController
{
    private readonly IWishlistService _wishlistService;

    public WishlistsController(IWishlistService wishlistService)
    {
        _wishlistService = wishlistService;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
        {
            return userId;
        }
        return 0; // Return 0 or throw exception
    }

    [HttpGet]
    public async Task<IActionResult> GetWishlist()
    {
        int userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var result = await _wishlistService.GetWishlistByUserIdAsync(userId);
        return HandleResult(result);
    }

    [HttpPost("items")]
    public async Task<IActionResult> AddItem([FromBody] AddWishlistItemRequest dto)
    {
        int userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var result = await _wishlistService.AddItemToWishlistAsync(userId, dto.ProductId);
        return HandleResult(result);
    }

    [HttpDelete("items/{productId}")]
    public async Task<IActionResult> RemoveItem(int productId)
    {
        int userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var result = await _wishlistService.RemoveItemFromWishlistAsync(userId, productId);
        return HandleResult(result);
    }

    [HttpDelete]
    public async Task<IActionResult> ClearWishlist()
    {
        int userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var result = await _wishlistService.ClearWishlistAsync(userId);
        return HandleResult(result);
    }
}
