using System.Security.Claims;
using EcommerceAPI.Application.Abstractions.ServiceContracts;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Entities.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceAPI.API.Controllers;

[ApiController]
[Route("api/v1/uploads")]
[Authorize]
public class UploadsController : BaseApiController
{
    private readonly IReturnAttachmentStorageService _returnAttachmentStorageService;

    public UploadsController(IReturnAttachmentStorageService returnAttachmentStorageService)
    {
        _returnAttachmentStorageService = returnAttachmentStorageService;
    }

    [HttpPost("return-photos")]
    [RequestSizeLimit(26_214_400)]
    public async Task<IActionResult> UploadReturnPhotos([FromForm] List<IFormFile> files, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
        {
            return Unauthorized();
        }

        if (files.Count == 0)
        {
            return HandleResult(new ErrorDataResult<List<UploadedReturnPhotoDto>>("Yüklenecek dosya bulunamadı."));
        }

        var uploads = new List<ReturnAttachmentUploadContent>();
        try
        {
            foreach (var file in files)
            {
                uploads.Add(new ReturnAttachmentUploadContent
                {
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    SizeBytes = file.Length,
                    Content = file.OpenReadStream()
                });
            }

            var result = await _returnAttachmentStorageService.UploadTemporaryPhotosAsync(userId, uploads, cancellationToken);
            return HandleResult(result);
        }
        finally
        {
            foreach (var upload in uploads)
            {
                upload.Content.Dispose();
            }
        }
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }
}
