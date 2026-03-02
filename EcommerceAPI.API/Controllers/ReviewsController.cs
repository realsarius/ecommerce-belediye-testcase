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

[ApiController]
[Route("api/v1/admin/reviews")]
[Authorize(Roles = "Admin")]
public class AdminReviewsController : BaseApiController
{
    private readonly IProductReviewService _reviewService;

    public AdminReviewsController(IProductReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    [HttpGet]
    public async Task<IActionResult> GetReviews([FromQuery] string? status = null)
    {
        Entities.Enums.ProductReviewModerationStatus? moderationStatus = null;

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<Entities.Enums.ProductReviewModerationStatus>(status, true, out var parsedStatus))
                return BadRequest(new { success = false, message = "Gecersiz yorum durumu." });

            moderationStatus = parsedStatus;
        }

        var result = await _reviewService.GetAdminReviewsAsync(moderationStatus);
        return HandleResult(result);
    }

    [HttpPut("{reviewId}/approve")]
    public async Task<IActionResult> ApproveReview(int reviewId)
    {
        var result = await _reviewService.AdminApproveAsync(reviewId, GetUserId());
        return HandleResult(result);
    }

    [HttpPut("{reviewId}/reject")]
    public async Task<IActionResult> RejectReview(int reviewId, [FromBody] ReviewModerationRequest request)
    {
        var result = await _reviewService.AdminRejectAsync(reviewId, GetUserId(), request);
        return HandleResult(result);
    }

    [HttpPut("bulk-approve")]
    public async Task<IActionResult> BulkApprove([FromBody] BulkApproveReviewsRequest request)
    {
        var result = await _reviewService.AdminBulkApproveAsync(request.Ids, GetUserId());
        return HandleResult(result);
    }

    private int GetUserId()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdStr, out var userId) ? userId : 0;
    }
}

[ApiController]
[Route("api/v1/seller/reviews")]
[Authorize(Roles = "Seller")]
public class SellerReviewsController : BaseApiController
{
    private readonly IProductReviewService _reviewService;

    public SellerReviewsController(IProductReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    [HttpPost("{reviewId}/reply")]
    public async Task<IActionResult> ReplyToReview(int reviewId, [FromBody] SellerReviewReplyRequest request)
    {
        var result = await _reviewService.SellerReplyAsync(reviewId: reviewId, sellerUserId: GetUserId(), request: request);
        return HandleResult(result);
    }

    private int GetUserId()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdStr, out var userId) ? userId : 0;
    }
}
