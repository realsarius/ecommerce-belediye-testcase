using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace EcommerceAPI.API.Controllers;

[ApiController]
[Route("api/v1/frontend-settings")]
[Authorize]
public class FrontendSettingsController : ControllerBase
{
    private readonly FrontendFeatureSettings _frontendFeatureSettings;

    public FrontendSettingsController(IOptions<FrontendFeatureSettings> frontendFeatureSettings)
    {
        _frontendFeatureSettings = frontendFeatureSettings.Value;
    }

    [HttpGet("features")]
    public IActionResult GetFeatures()
    {
        return Ok(new SuccessDataResult<FrontendFeatureSettingsDto>(new FrontendFeatureSettingsDto
        {
            EnableCheckoutLegalConsents = _frontendFeatureSettings.EnableCheckoutLegalConsents,
            EnableCheckoutInvoiceInfo = _frontendFeatureSettings.EnableCheckoutInvoiceInfo,
            EnableShipmentTimeline = _frontendFeatureSettings.EnableShipmentTimeline,
            EnableReturnAttachments = _frontendFeatureSettings.EnableReturnAttachments,
        }));
    }
}
