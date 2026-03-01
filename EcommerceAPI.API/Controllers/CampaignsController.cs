using EcommerceAPI.Business.Abstract;
using Microsoft.AspNetCore.Mvc;

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
}
