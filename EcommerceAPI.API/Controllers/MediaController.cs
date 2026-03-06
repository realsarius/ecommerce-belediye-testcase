using System.Security.Claims;
using EcommerceAPI.Application.Abstractions.ServiceContracts;
using EcommerceAPI.Entities.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceAPI.API.Controllers;

[ApiController]
[Route("api/v1/media")]
[Authorize]
public class MediaController : BaseApiController
{
    private readonly IMediaUploadService _mediaUploadService;

    public MediaController(IMediaUploadService mediaUploadService)
    {
        _mediaUploadService = mediaUploadService;
    }

    [HttpPost("presign")]
    public async Task<IActionResult> PresignUpload([FromBody] PresignMediaUploadRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
        {
            return Unauthorized();
        }

        var result = await _mediaUploadService.GetPresignedUploadUrlAsync(
            userId,
            User.IsInRole("Admin"),
            request);

        return HandleResult(result);
    }

    [HttpPost("confirm")]
    public async Task<IActionResult> ConfirmUpload([FromBody] ConfirmMediaUploadRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
        {
            return Unauthorized();
        }

        var result = await _mediaUploadService.ConfirmUploadAsync(
            userId,
            User.IsInRole("Admin"),
            request);

        return HandleResult(result);
    }

    [HttpDelete("{imageId:int}")]
    public async Task<IActionResult> DeleteImage(int imageId)
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
        {
            return Unauthorized();
        }

        var result = await _mediaUploadService.DeleteProductImageAsync(
            userId,
            User.IsInRole("Admin"),
            imageId);

        return HandleDeleteResult(result);
    }

    [HttpPut("products/{productId:int}/images/reorder")]
    public async Task<IActionResult> ReorderProductImages(int productId, [FromBody] ReorderProductImagesRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
        {
            return Unauthorized();
        }

        var result = await _mediaUploadService.ReorderProductImagesAsync(
            userId,
            User.IsInRole("Admin"),
            productId,
            request);

        return HandleResult(result);
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }
}
