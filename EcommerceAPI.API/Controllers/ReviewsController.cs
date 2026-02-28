using System.Security.Claims;
using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceAPI.API.Controllers;

[ApiController]
[Route("api/v1/products/{productId}/reviews")]
public class ReviewsController : BaseApiController
{
    private readonly IProductReviewService _reviewService;

    public ReviewsController(IProductReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    [HttpGet]
    public async Task<IActionResult> GetReviews(int productId)
    {
        var result = await _reviewService.GetByProductIdAsync(productId);
        if (result.Success)
            return Ok(result);
        return BadRequest(result);
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetReviewSummary(int productId)
    {
        var result = await _reviewService.GetProductReviewSummaryAsync(productId);
        if (result.Success)
            return Ok(result);
        return BadRequest(result);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateReview(int productId, [FromBody] CreateReviewRequest request)
    {
        var userId = GetUserId();
        var result = await _reviewService.CreateAsync(userId, productId, request);
        if (result.Success)
            return CreatedAtAction(nameof(GetReviews), new { productId }, result);
        return BadRequest(result);
    }

    [HttpPut("{reviewId}")]
    [Authorize]
    public async Task<IActionResult> UpdateReview(int productId, int reviewId, [FromBody] UpdateReviewRequest request)
    {
        var userId = GetUserId();
        var result = await _reviewService.UpdateAsync(userId, reviewId, request);
        if (result.Success)
            return Ok(result);
        return BadRequest(result);
    }

    [HttpDelete("{reviewId}")]
    [Authorize]
    public async Task<IActionResult> DeleteReview(int productId, int reviewId)
    {
        var userId = GetUserId();
        var result = await _reviewService.DeleteAsync(userId, reviewId);
        if (result.Success)
            return Ok(result);
        return BadRequest(result);
    }

    protected int GetUserId()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdStr, out var userId))
        {
            return userId;
        }
        return 0;
    }

    [HttpGet("can-review")]
    [Authorize]
    public async Task<IActionResult> CanUserReview(int productId)
    {
        var userId = GetUserId();
        var result = await _reviewService.CanUserReviewAsync(userId, productId);
        if (result.Success)
            return Ok(result);
        return BadRequest(result);
    }
}
