using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Entities.DTOs;

public class SupportConversationDto : IDto
{
    public int Id { get; set; }
    public string Subject { get; set; } = string.Empty;

    public int CustomerUserId { get; set; }
    public string CustomerName { get; set; } = string.Empty;

    public int? SupportUserId { get; set; }
    public string? SupportName { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? LastMessage { get; set; }
    public string? LastSenderRole { get; set; }
    public DateTime? LastMessageAt { get; set; }

    public DateTime? ClosedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
