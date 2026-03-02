using System.Security.Claims;
using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.DTOs;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceAPI.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/announcements")]
public class AdminAnnouncementsController : BaseApiController
{
    private readonly IAnnouncementService _announcementService;
    private readonly IBackgroundJobClient _backgroundJobClient;

    public AdminAnnouncementsController(
        IAnnouncementService announcementService,
        IBackgroundJobClient backgroundJobClient)
    {
        _announcementService = announcementService;
        _backgroundJobClient = backgroundJobClient;
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

        var scheduledAt = created.Data.ScheduledAt;
        if (scheduledAt.HasValue && scheduledAt.Value > DateTime.UtcNow.AddMinutes(1))
        {
            _backgroundJobClient.Schedule<IAnnouncementService>(
                service => service.SendAnnouncementAsync(created.Data.Id),
                scheduledAt.Value);
        }
        else
        {
            await _announcementService.SendAnnouncementAsync(created.Data.Id);
        }

        var latest = await _announcementService.GetByIdAsync(created.Data.Id);
        return HandleResult(latest);
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null && int.TryParse(claim.Value, out var userId) ? userId : 0;
    }
}
