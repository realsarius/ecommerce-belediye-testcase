using EcommerceAPI.Business.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceAPI.API.Controllers;

[ApiController]
[Route("api/v1/seller/dashboard")]
[Authorize(Roles = "Seller")]
public class SellerDashboardController : SellerApiControllerBase
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
        var sellerContext = await GetSellerContextAsync(_sellerProfileService);
        if (sellerContext == null)
        {
            return InvalidSellerSession();
        }
        if (sellerContext.SellerProfileId == null)
        {
            return MissingSellerProfile();
        }

        var result = await _sellerAnalyticsService.GetDashboardKpiAsync(sellerContext.SellerProfileId.Value, days);
        return HandleResult(result);
    }

    [HttpGet("revenue-trend")]
    public async Task<IActionResult> GetRevenueTrend([FromQuery] string period = "daily")
    {
        var sellerContext = await GetSellerContextAsync(_sellerProfileService);
        if (sellerContext == null)
        {
            return InvalidSellerSession();
        }
        if (sellerContext.SellerProfileId == null)
        {
            return MissingSellerProfile();
        }

        var result = await _sellerAnalyticsService.GetDashboardRevenueTrendAsync(sellerContext.SellerProfileId.Value, period);
        return HandleResult(result);
    }

    [HttpGet("order-status-distribution")]
    public async Task<IActionResult> GetOrderStatusDistribution()
    {
        var sellerContext = await GetSellerContextAsync(_sellerProfileService);
        if (sellerContext == null)
        {
            return InvalidSellerSession();
        }
        if (sellerContext.SellerProfileId == null)
        {
            return MissingSellerProfile();
        }

        var result = await _sellerAnalyticsService.GetDashboardOrderStatusDistributionAsync(sellerContext.SellerProfileId.Value);
        return HandleResult(result);
    }

    [HttpGet("product-performance")]
    public async Task<IActionResult> GetProductPerformance([FromQuery] int take = 5)
    {
        var sellerContext = await GetSellerContextAsync(_sellerProfileService);
        if (sellerContext == null)
        {
            return InvalidSellerSession();
        }
        if (sellerContext.SellerProfileId == null)
        {
            return MissingSellerProfile();
        }

        var result = await _sellerAnalyticsService.GetDashboardProductPerformanceAsync(sellerContext.SellerProfileId.Value, take);
        return HandleResult(result);
    }

    [HttpGet("recent-orders")]
    public async Task<IActionResult> GetRecentOrders([FromQuery] int take = 5)
    {
        var sellerContext = await GetSellerContextAsync(_sellerProfileService);
        if (sellerContext == null)
        {
            return InvalidSellerSession();
        }
        if (sellerContext.SellerProfileId == null)
        {
            return MissingSellerProfile();
        }

        var result = await _sellerAnalyticsService.GetDashboardRecentOrdersAsync(sellerContext.SellerProfileId.Value, take);
        return HandleResult(result);
    }
}
