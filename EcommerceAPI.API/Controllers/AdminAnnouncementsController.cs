using System.Security.Claims;
using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.IntegrationEvents;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceAPI.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/announcements")]
public class AdminAnnouncementsController : BaseApiController
{
    private readonly IAnnouncementService _announcementService;
    private readonly IPublishEndpoint _publishEndpoint;

    public AdminAnnouncementsController(
        IAnnouncementService announcementService,
        IPublishEndpoint publishEndpoint)
    {
        _announcementService = announcementService;
        _publishEndpoint = publishEndpoint;
    }

    [HttpGet]
    public async Task<IActionResult> GetRecent([FromQuery] int take = 20)
    {
        var result = await _announcementService.GetRecentAsync(take);
        return HandleResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAnnouncementRequest request)
    {
        var adminUserId = GetCurrentUserId();
        if (adminUserId <= 0)
        {
            return BadRequest(new { success = false, message = "Geçersiz admin kullanıcısı." });
        }

        var created = await _announcementService.CreateAsync(adminUserId, request);
        if (!created.Success)
        {
            return HandleResult(created);
        }

        await _publishEndpoint.Publish(new AnnouncementCreatedEvent
        {
            AnnouncementId = created.Data.Id,
            ScheduledAt = created.Data.ScheduledAt
        });

        var latest = await _announcementService.GetByIdAsync(created.Data.Id);
        return HandleResult(latest);
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null && int.TryParse(claim.Value, out var userId) ? userId : 0;
    }
}
