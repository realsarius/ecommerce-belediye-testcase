using System.Security.Claims;
using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace EcommerceAPI.API.Hubs;

[Authorize]
public class LiveSupportHub : Hub
{
    private readonly ISupportConversationService _supportConversationService;

    public LiveSupportHub(ISupportConversationService supportConversationService)
    {
        _supportConversationService = supportConversationService;
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
}
