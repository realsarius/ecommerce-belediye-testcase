using System.Security.Claims;
using EcommerceAPI.Business.Abstract;
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

    public ReturnsController(IReturnRequestService returnRequestService)
    {
        _returnRequestService = returnRequestService;
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
}
