using System.Security.Claims;
using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Entities.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceAPI.API.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
public class ReturnsController : BaseApiController
{
    private readonly IReturnRequestService _returnRequestService;
    private readonly IReturnAttachmentAccessService _returnAttachmentAccessService;

    public ReturnsController(
        IReturnRequestService returnRequestService,
        IReturnAttachmentAccessService returnAttachmentAccessService)
    {
        _returnRequestService = returnRequestService;
        _returnAttachmentAccessService = returnAttachmentAccessService;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    [HttpPost("orders/{orderId:int:min(1)}/returns")]
    public async Task<IActionResult> CreateReturnRequest(int orderId, [FromBody] CreateReturnRequestRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var result = await _returnRequestService.CreateReturnRequestAsync(userId, orderId, request);
        return HandleResult(result);
    }

    [HttpGet("returns/mine")]
    public async Task<IActionResult> GetMyReturnRequests()
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var result = await _returnRequestService.GetUserReturnRequestsAsync(userId);
        return HandleResult(result);
    }

    [HttpGet("returns/{returnRequestId:int:min(1)}/attachments/{attachmentId:int:min(1)}/access-url")]
    public async Task<IActionResult> GetAttachmentAccessUrl(int returnRequestId, int attachmentId)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var role = User.FindFirstValue(ClaimTypes.Role);
        var baseUrl = $"{Request.Scheme}://{Request.Host.Value}";

        var result = await _returnAttachmentAccessService.CreateSignedAccessUrlAsync(
            userId,
            role,
            returnRequestId,
            attachmentId,
            baseUrl);

        return HandleResult(result);
    }

    [AllowAnonymous]
    [HttpGet("returns/attachments/{attachmentId:int:min(1)}/content")]
    public async Task<IActionResult> GetAttachmentContent(int attachmentId, [FromQuery] string token)
    {
        var result = await _returnAttachmentAccessService.ValidateAccessTokenAsync(attachmentId, token);
        if (!result.Success)
        {
            return HandleResult(new ErrorDataResult<object>(result.Message));
        }

        var (attachment, absolutePath) = result.Data;
        var stream = new FileStream(absolutePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return File(stream, attachment.ContentType, enableRangeProcessing: false);
    }
}
