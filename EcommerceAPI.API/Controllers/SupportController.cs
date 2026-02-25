using System.Security.Claims;
using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EcommerceAPI.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class SupportController : ControllerBase
{
    private readonly ISupportConversationService _supportConversationService;

    public SupportController(ISupportConversationService supportConversationService)
    {
        _supportConversationService = supportConversationService;
    }

    [HttpPost("conversations")]
    public async Task<IActionResult> StartConversation([FromBody] StartSupportConversationRequest request)
    {
        var userId = GetUserId();
        var result = await _supportConversationService.GetOrCreateConversationAsync(userId, request);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    [HttpGet("conversations/my")]
    public async Task<IActionResult> GetMyConversations()
    {
        var userId = GetUserId();
        var role = GetUserRole();
        var result = await _supportConversationService.GetMyConversationsAsync(userId, role);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    [HttpGet("conversations/queue")]
    [Authorize(Roles = "Admin,Support")]
    public async Task<IActionResult> GetQueue([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userId = GetUserId();
        var role = GetUserRole();
        var result = await _supportConversationService.GetQueueAsync(userId, role, page, pageSize);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    [HttpGet("conversations/{conversationId:int}/messages")]
    public async Task<IActionResult> GetMessages(int conversationId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var userId = GetUserId();
        var role = GetUserRole();
        var result = await _supportConversationService.GetMessagesAsync(conversationId, userId, role, page, pageSize);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    [EnableRateLimiting("support-message-http")]
    [HttpPost("conversations/{conversationId:int}/messages")]
    public async Task<IActionResult> SendMessage(int conversationId, [FromBody] SendSupportMessageRequest request)
    {
        var userId = GetUserId();
        var role = GetUserRole();
        var result = await _supportConversationService.SendMessageAsync(conversationId, userId, role, request);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    [HttpPost("conversations/{conversationId:int}/assign")]
    [Authorize(Roles = "Admin,Support")]
    public async Task<IActionResult> AssignConversation(int conversationId, [FromBody] AssignSupportConversationRequest request)
    {
        var userId = GetUserId();
        var role = GetUserRole();
        var result = await _supportConversationService.AssignConversationAsync(conversationId, request, userId, role);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    [HttpPost("conversations/{conversationId:int}/close")]
    public async Task<IActionResult> CloseConversation(int conversationId)
    {
        var userId = GetUserId();
        var role = GetUserRole();
        var result = await _supportConversationService.CloseConversationAsync(conversationId, userId, role);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    private int GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(claim, out var userId))
            throw new UnauthorizedAccessException("Geçersiz kullanıcı");
        return userId;
    }

    private string GetUserRole()
    {
        return User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
    }
}
