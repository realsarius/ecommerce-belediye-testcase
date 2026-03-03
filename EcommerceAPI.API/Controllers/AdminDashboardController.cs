using EcommerceAPI.Business.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceAPI.API.Controllers;

[ApiController]
[Route("api/v1/admin/dashboard")]
[Authorize(Roles = "Admin")]
public class AdminDashboardController : BaseApiController
{
    private readonly IAdminDashboardService _adminDashboardService;

    public AdminDashboardController(IAdminDashboardService adminDashboardService)
    {
        _adminDashboardService = adminDashboardService;
    }

    [HttpGet("kpi")]
    public async Task<IActionResult> GetKpi()
    {
        var result = await _adminDashboardService.GetKpiAsync();
        return HandleResult(result);
    }

    [HttpGet("revenue-trend")]
    public async Task<IActionResult> GetRevenueTrend([FromQuery] string period = "daily")
    {
        var result = await _adminDashboardService.GetRevenueTrendAsync(period);
        return HandleResult(result);
    }

    [HttpGet("category-sales")]
    public async Task<IActionResult> GetCategorySales()
    {
        var result = await _adminDashboardService.GetCategorySalesAsync();
        return HandleResult(result);
    }

    [HttpGet("user-registrations")]
    public async Task<IActionResult> GetUserRegistrations([FromQuery] int days = 30)
    {
        var result = await _adminDashboardService.GetUserRegistrationsAsync(days);
        return HandleResult(result);
    }

    [HttpGet("order-status-distribution")]
    public async Task<IActionResult> GetOrderStatusDistribution()
    {
        var result = await _adminDashboardService.GetOrderStatusDistributionAsync();
        return HandleResult(result);
    }

    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStock([FromQuery] int threshold = 5)
    {
        var result = await _adminDashboardService.GetLowStockAsync(threshold);
        return HandleResult(result);
    }

    [HttpGet("recent-orders")]
    public async Task<IActionResult> GetRecentOrders([FromQuery] int limit = 5)
    {
        var result = await _adminDashboardService.GetRecentOrdersAsync(limit);
        return HandleResult(result);
    }
}
