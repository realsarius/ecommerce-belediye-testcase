using System.Security.Claims;
using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EcommerceAPI.API.Controllers;

[ApiController]
[Route("api/v1/search")]
public class SearchController : ControllerBase
{
    private readonly IProductSearchService _productSearchService;
    private readonly IRecommendationService _recommendationService;

    public SearchController(IProductSearchService productSearchService, IRecommendationService recommendationService)
    {
        _productSearchService = productSearchService;
        _recommendationService = recommendationService;
    }

    [HttpGet("products")]
    [EnableRateLimiting("search")]
    public async Task<IActionResult> SearchProducts(
        [FromQuery] ProductListRequest request,
        [FromQuery(Name = "q")] string? q)
    {
        if (!string.IsNullOrWhiteSpace(q))
        {
            request.Search = q;
        }

        var result = await _productSearchService.SearchProductsAsync(request);
        var userId = GetCurrentUserId();
        if (result.Success && userId.HasValue && !string.IsNullOrWhiteSpace(request.Search))
        {
            try
            {
                await _recommendationService.TrackSearchQueryAsync(userId.Value, request.Search!, HttpContext.RequestAborted);
            }
            catch
            {
                // Search response should not fail if personalization tracking is temporarily unavailable.
            }
        }

        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    [HttpGet("suggestions")]
    [EnableRateLimiting("search")]
    public async Task<IActionResult> SuggestProducts(
        [FromQuery(Name = "q")] string? q,
        [FromQuery] int limit = 8)
    {
        var result = await _productSearchService.SuggestProductsAsync(q ?? string.Empty, limit);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
