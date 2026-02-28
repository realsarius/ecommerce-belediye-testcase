using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace EcommerceAPI.API.Controllers;

[Route("api/v1/wishlists/price-alerts")]
[ApiController]
[Authorize]
public class WishlistPriceAlertsController : BaseApiController
{
    private readonly IWishlistPriceAlertService _wishlistPriceAlertService;

    public WishlistPriceAlertsController(IWishlistPriceAlertService wishlistPriceAlertService)
    {
        _wishlistPriceAlertService = wishlistPriceAlertService;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    [HttpGet]
    [EnableRateLimiting("wishlist-read")]
    public async Task<IActionResult> GetPriceAlerts()
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var result = await _wishlistPriceAlertService.GetUserPriceAlertsAsync(userId);
        return HandleResult(result);
    }

    [HttpPut]
    [EnableRateLimiting("wishlist")]
    public async Task<IActionResult> UpsertPriceAlert([FromBody] UpsertWishlistPriceAlertRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var result = await _wishlistPriceAlertService.UpsertPriceAlertAsync(userId, request);
        return HandleResult(result);
    }

    [HttpDelete("{productId:int:min(1)}")]
    [EnableRateLimiting("wishlist")]
    public async Task<IActionResult> RemovePriceAlert(int productId)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var result = await _wishlistPriceAlertService.RemovePriceAlertAsync(userId, productId);
        return HandleResult(result);
    }
}
