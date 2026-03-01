using System.Security.Claims;
using EcommerceAPI.Business.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceAPI.API.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
public class NotificationsController : BaseApiController
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    [HttpGet("notifications")]
    public async Task<IActionResult> GetMyNotifications([FromQuery] int take = 50)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var result = await _notificationService.GetUserNotificationsAsync(userId, take);
        return HandleResult(result);
    }

    [HttpGet("notifications/unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var result = await _notificationService.GetUnreadCountAsync(userId);
        return HandleResult(result);
    }

    [HttpPost("notifications/{notificationId:int:min(1)}/read")]
    public async Task<IActionResult> MarkAsRead(int notificationId)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var result = await _notificationService.MarkAsReadAsync(userId, notificationId);
        return HandleResult(result);
    }

    [HttpPost("notifications/read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var result = await _notificationService.MarkAllAsReadAsync(userId);
        return HandleResult(result);
    }
}
