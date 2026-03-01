using System.Security.Claims;
using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceAPI.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly IRecommendationService _recommendationService;

    public ProductsController(IProductService productService, IRecommendationService recommendationService)
    {
        _productService = productService;
        _recommendationService = recommendationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetProducts(
        [FromQuery] ProductListRequest request)
    {
        var result = await _productService.GetProductsAsync(request);
        if (result.Success)
        {
            return Ok(result);
        }
        return BadRequest(result);
    }

    [HttpGet("{id}", Name = "GetProductById")]
    public async Task<IActionResult> GetProduct(int id)
    {
        var result = await _productService.GetProductByIdAsync(id);
        
        if (result.Success)
        {
            return Ok(result);
        }
        return BadRequest(result);
    }

    [HttpPost("{id:int:min(1)}/views")]
    public async Task<IActionResult> TrackView(int id, [FromHeader(Name = "X-Session-Id")] string? sessionId)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userId = int.TryParse(userIdClaim, out var parsedUserId) ? parsedUserId : (int?)null;

        var result = await _recommendationService.TrackProductViewAsync(id, userId, sessionId, HttpContext.RequestAborted);
        if (result.Success)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }

    [HttpGet("{id:int:min(1)}/recommendations/also-viewed")]
    public async Task<IActionResult> GetAlsoViewed(int id, [FromQuery] int take = 4)
    {
        var result = await _recommendationService.GetAlsoViewedProductsAsync(id, take, HttpContext.RequestAborted);
        if (result.Success)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }

    [HttpGet("{id:int:min(1)}/recommendations/frequently-bought")]
    public async Task<IActionResult> GetFrequentlyBoughtTogether(int id, [FromQuery] int take = 4)
    {
        var result = await _recommendationService.GetFrequentlyBoughtTogetherProductsAsync(id, take, HttpContext.RequestAborted);
        if (result.Success)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }

    [HttpPost("{id:int:min(1)}/recommendations/click")]
    public async Task<IActionResult> TrackRecommendationClick(int id, [FromBody] TrackRecommendationClickRequest request, [FromHeader(Name = "X-Session-Id")] string? sessionId)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userId = int.TryParse(userIdClaim, out var parsedUserId) ? parsedUserId : (int?)null;

        var result = await _recommendationService.TrackRecommendationClickAsync(
            id,
            request.TargetProductId,
            request.Source,
            userId,
            sessionId,
            HttpContext.RequestAborted);

        if (result.Success)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }
}
