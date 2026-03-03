using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Entities.DTOs;

public class AnnouncementDto : IDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string AudienceType { get; set; } = string.Empty;
    public string? TargetRole { get; set; }
    public List<int> TargetUserIds { get; set; } = new();
    public List<string> Channels { get; set; } = new();
    public string Status { get; set; } = string.Empty;
    public int RecipientCount { get; set; }
    public int DeliveredCount { get; set; }
    public int FailedCount { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
}

public class CreateAnnouncementRequest : IDto
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string AudienceType { get; set; } = string.Empty;
    public string? TargetRole { get; set; }
    public List<int> TargetUserIds { get; set; } = new();
    public List<string> Channels { get; set; } = new();
    public DateTime? ScheduledAt { get; set; }
}
