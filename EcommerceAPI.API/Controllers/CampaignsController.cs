using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EcommerceAPI.API.Controllers;

[ApiController]
[Route("api/v1/campaigns")]
public class CampaignsController : ControllerBase
{
    private readonly ICampaignService _campaignService;

    public CampaignsController(ICampaignService campaignService)
    {
        _campaignService = campaignService;
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActiveCampaigns()
    {
        var result = await _campaignService.GetActiveAsync();
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{id:int:min(1)}/interactions")]
    public async Task<IActionResult> TrackInteraction(
        int id,
        [FromBody] TrackCampaignInteractionRequest request,
        [FromHeader(Name = "X-Session-Id")] string? sessionId)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userId = int.TryParse(userIdClaim, out var parsedUserId) ? parsedUserId : (int?)null;

        var result = await _campaignService.TrackInteractionAsync(
            id,
            request.InteractionType,
            request.ProductId,
            userId,
            sessionId);

        return result.Success ? Ok(result) : BadRequest(result);
    }
}
