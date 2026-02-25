using System.Security.Claims;
using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;

namespace EcommerceAPI.API.Hubs;

[Authorize]
public class LiveSupportHub : Hub
{
    private readonly ISupportConversationService _supportConversationService;
    private readonly IConnectionMultiplexer _redis;
    private const int SupportSendPermitLimit = 20;
    private static readonly TimeSpan SupportSendWindow = TimeSpan.FromMinutes(1);


    public LiveSupportHub(ISupportConversationService supportConversationService, IConnectionMultiplexer redis)
    {
        _supportConversationService = supportConversationService;
        _redis = redis;
    }

    public async Task JoinConversation(int conversationId)
    {
        var userId = GetUserId();
        var role = GetUserRole();

        var canAccess = await _supportConversationService.CanAccessConversationAsync(conversationId, userId, role);
        if (!canAccess)
            throw new HubException("Bu görüşmeye erişim yetkiniz yok");

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(conversationId));
        await Clients.Caller.SendAsync("JoinedConversation", conversationId);
    }

    public async Task SendMessage(int conversationId, string message)
    {
        var userId = GetUserId();
        var role = GetUserRole();

        if (!await TryAcquireSendPermitAsync(userId))
            throw new HubException("RATE_LIMIT_EXCEEDED");

        var result = await _supportConversationService.SendMessageAsync(
            conversationId,
            userId,
            role,
            new SendSupportMessageRequest { Message = message });

        if (!result.Success || result.Data == null)
            throw new HubException(result.Message ?? "Mesaj gönderilemedi");

        await Clients.Group(GroupName(conversationId)).SendAsync("ReceiveMessage", result.Data);
    }

    public async Task CloseConversation(int conversationId)
    {
        var userId = GetUserId();
        var role = GetUserRole();

        var result = await _supportConversationService.CloseConversationAsync(conversationId, userId, role);
        if (!result.Success || result.Data == null)
            throw new HubException(result.Message ?? "Görüşme kapatılamadı");

        await Clients.Group(GroupName(conversationId)).SendAsync("ConversationClosed", result.Data);
    }

    public async Task AssignConversation(int conversationId, int supportUserId)
    {
        var userId = GetUserId();
        var role = GetUserRole();

        var result = await _supportConversationService.AssignConversationAsync(
            conversationId,
            new AssignSupportConversationRequest { SupportUserId = supportUserId },
            userId,
            role);

        if (!result.Success || result.Data == null)
            throw new HubException(result.Message ?? "Atama yapılamadı");

        await Clients.Group(GroupName(conversationId)).SendAsync("ConversationAssigned", result.Data);
    }

    private int GetUserId()
    {
        var claim = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(claim, out var userId))
            throw new HubException("Geçersiz kullanıcı");
        return userId;
    }

    private string GetUserRole()
    {
        return Context.User?.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
    }

    private static string GroupName(int conversationId) => $"support-conv-{conversationId}";

    private async Task<bool> TryAcquireSendPermitAsync(int userId)
    {
        var db = _redis.GetDatabase();
        var bucket = DateTime.UtcNow.ToString("yyyyMMddHHmm");
        var key = $"ratelimit:support:send:{userId}:{bucket}";

        var count = await db.StringIncrementAsync(key);
        if (count == 1)
        {
            await db.KeyExpireAsync(key, SupportSendWindow + TimeSpan.FromSeconds(5));
        }

        return count <= SupportSendPermitLimit;
    }

}
