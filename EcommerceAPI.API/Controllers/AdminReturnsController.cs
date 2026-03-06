using EcommerceAPI.Application.Abstractions.ServiceContracts;
using EcommerceAPI.Entities.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceAPI.API.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/v1/admin/returns")]
public class AdminReturnsController : BaseApiController
{
    private readonly IReturnRequestService _returnRequestService;

    public AdminReturnsController(
        IReturnRequestService returnRequestService)
    {
        _returnRequestService = returnRequestService;
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return userIdClaim != null && int.TryParse(userIdClaim.Value, out var parsedUserId) ? parsedUserId : null;
    }

    [HttpGet]
    public async Task<IActionResult> GetReturnRequests([FromQuery] string? status = null)
    {
        var result = await _returnRequestService.GetReturnRequestsAsync(status);
        return HandleResult(result);
    }

    [HttpPut("{id:int:min(1)}/approve")]
    public async Task<IActionResult> ApproveReturnRequest(int id, [FromBody] ReviewReturnRequestRequest request)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        request.Status = "Approved";
        var result = await _returnRequestService.ReviewReturnRequestAsync(id, userId.Value, request);
        return HandleResult(result);
    }

    [HttpPut("{id:int:min(1)}/reject")]
    public async Task<IActionResult> RejectReturnRequest(int id, [FromBody] ReviewReturnRequestRequest request)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        request.Status = "Rejected";
        var result = await _returnRequestService.ReviewReturnRequestAsync(id, userId.Value, request);
        return HandleResult(result);
    }
}
