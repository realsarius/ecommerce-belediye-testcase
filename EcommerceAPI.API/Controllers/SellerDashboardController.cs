using System.Security.Claims;
using EcommerceAPI.Business.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceAPI.API.Controllers;

[ApiController]
[Route("api/v1/seller/dashboard")]
[Authorize(Roles = "Seller")]
public class SellerDashboardController : ControllerBase
{
    private readonly ISellerAnalyticsService _sellerAnalyticsService;
    private readonly ISellerProfileService _sellerProfileService;

    public SellerDashboardController(
        ISellerAnalyticsService sellerAnalyticsService,
        ISellerProfileService sellerProfileService)
    {
        _sellerAnalyticsService = sellerAnalyticsService;
        _sellerProfileService = sellerProfileService;
    }

    [HttpGet("kpi")]
    public async Task<IActionResult> GetKpi([FromQuery] int days = 30)
    {
        var sellerId = await ResolveSellerIdAsync();
        if (sellerId == null)
        {
            return BadRequest(new { message = "Satıcı profili bulunamadı." });
        }

        var result = await _sellerAnalyticsService.GetDashboardKpiAsync(sellerId.Value, days);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("revenue-trend")]
    public async Task<IActionResult> GetRevenueTrend([FromQuery] string period = "daily")
    {
        var sellerId = await ResolveSellerIdAsync();
        if (sellerId == null)
        {
            return BadRequest(new { message = "Satıcı profili bulunamadı." });
        }

        var result = await _sellerAnalyticsService.GetDashboardRevenueTrendAsync(sellerId.Value, period);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("order-status-distribution")]
    public async Task<IActionResult> GetOrderStatusDistribution()
    {
        var sellerId = await ResolveSellerIdAsync();
        if (sellerId == null)
        {
            return BadRequest(new { message = "Satıcı profili bulunamadı." });
        }

        var result = await _sellerAnalyticsService.GetDashboardOrderStatusDistributionAsync(sellerId.Value);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("product-performance")]
    public async Task<IActionResult> GetProductPerformance([FromQuery] int take = 5)
    {
        var sellerId = await ResolveSellerIdAsync();
        if (sellerId == null)
        {
            return BadRequest(new { message = "Satıcı profili bulunamadı." });
        }

        var result = await _sellerAnalyticsService.GetDashboardProductPerformanceAsync(sellerId.Value, take);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("recent-orders")]
    public async Task<IActionResult> GetRecentOrders([FromQuery] int take = 5)
    {
        var sellerId = await ResolveSellerIdAsync();
        if (sellerId == null)
        {
            return BadRequest(new { message = "Satıcı profili bulunamadı." });
        }

        var result = await _sellerAnalyticsService.GetDashboardRecentOrdersAsync(sellerId.Value, take);
        return result.Success ? Ok(result) : BadRequest(result);
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
