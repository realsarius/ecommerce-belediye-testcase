using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Entities.DTOs;

public class SupportMessageDto : IDto
{
    public int Id { get; set; }
    public int ConversationId { get; set; }

    public int SenderUserId { get; set; }
    public string SenderRole { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
    public bool IsSystemMessage { get; set; }

    public DateTime CreatedAt { get; set; }
}
