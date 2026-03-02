using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceAPI.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/notifications")]
public class AdminNotificationsController : BaseApiController
{
    private readonly INotificationPreferenceService _notificationPreferenceService;

    public AdminNotificationsController(INotificationPreferenceService notificationPreferenceService)
    {
        _notificationPreferenceService = notificationPreferenceService;
    }

    [HttpGet("templates")]
    public async Task<IActionResult> GetTemplates()
    {
        var result = await _notificationPreferenceService.GetTemplatesAsync();
        return HandleResult(result);
    }

    [HttpPut("templates/{type}")]
    public async Task<IActionResult> UpdateTemplate(string type, [FromBody] UpdateNotificationTemplateRequest request)
    {
        var result = await _notificationPreferenceService.UpdateTemplateAsync(type, request);
        return HandleResult(result);
    }
}
