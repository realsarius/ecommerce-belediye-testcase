using EcommerceAPI.Business.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceAPI.API.Controllers;

[ApiController]
[Route("api/v1/seller/analytics")]
[Authorize(Roles = "Seller")]
public class SellerAnalyticsController : SellerApiControllerBase
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
        var sellerContext = await GetSellerContextAsync(_sellerProfileService);
        if (sellerContext == null)
        {
            return InvalidSellerSession();
        }
        if (sellerContext.SellerProfileId == null)
        {
            return MissingSellerProfile();
        }

        var result = await _sellerAnalyticsService.GetSummaryAsync(sellerContext.SellerProfileId.Value);
        return HandleResult(result);
    }

    [HttpGet("trends")]
    public async Task<IActionResult> GetTrends([FromQuery] int days = 30)
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

        var result = await _sellerAnalyticsService.GetTrendAsync(sellerContext.SellerProfileId.Value, days);
        return HandleResult(result);
    }

    [HttpGet("finance")]
    public async Task<IActionResult> GetFinance([FromQuery] int days = 30)
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

        var result = await _sellerAnalyticsService.GetFinanceSummaryAsync(sellerContext.SellerProfileId.Value, days);
        return HandleResult(result);
    }
}
