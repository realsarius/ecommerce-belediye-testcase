using System.Security.Claims;
using EcommerceAPI.Business.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceAPI.API.Controllers;

[ApiController]
[Route("api/v1/seller/analytics")]
[Authorize(Roles = "Seller")]
public class SellerAnalyticsController : ControllerBase
{
    private readonly ISellerAnalyticsService _sellerAnalyticsService;
    private readonly ISellerProfileService _sellerProfileService;

    public SellerAnalyticsController(
        ISellerAnalyticsService sellerAnalyticsService,
        ISellerProfileService sellerProfileService)
    {
        _sellerAnalyticsService = sellerAnalyticsService;
        _sellerProfileService = sellerProfileService;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var sellerId = await ResolveSellerIdAsync();
        if (sellerId == null)
        {
            return BadRequest(new { message = "Satıcı profili bulunamadı." });
        }

        var result = await _sellerAnalyticsService.GetSummaryAsync(sellerId.Value);
        if (result.Success)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }

    [HttpGet("trends")]
    public async Task<IActionResult> GetTrends([FromQuery] int days = 30)
    {
        var sellerId = await ResolveSellerIdAsync();
        if (sellerId == null)
        {
            return BadRequest(new { message = "Satıcı profili bulunamadı." });
        }

        var result = await _sellerAnalyticsService.GetTrendAsync(sellerId.Value, days);
        if (result.Success)
        {
            return Ok(result);
        }

        return BadRequest(result);
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
