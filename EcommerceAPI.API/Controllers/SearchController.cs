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

    public SearchController(IProductSearchService productSearchService)
    {
        _productSearchService = productSearchService;
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
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }
}
