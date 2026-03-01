using System.Security.Claims;
using EcommerceAPI.Business.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceAPI.API.Controllers;

[Route("api/v1/[controller]")]
[Authorize]
public class LoyaltyController : BaseApiController
{
    private readonly ILoyaltyService _loyaltyService;

    public LoyaltyController(ILoyaltyService loyaltyService)
    {
        _loyaltyService = loyaltyService;
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var userId))
        {
            throw new UnauthorizedAccessException("Geçersiz kullanıcı kimliği");
        }

        return userId;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var result = await _loyaltyService.GetSummaryAsync(GetCurrentUserId());
        return HandleResult(result);
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] int limit = 50)
    {
        var result = await _loyaltyService.GetHistoryAsync(GetCurrentUserId(), Math.Clamp(limit, 1, 100));
        return HandleResult(result);
    }
}
