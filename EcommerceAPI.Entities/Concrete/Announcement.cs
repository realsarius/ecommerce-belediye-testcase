using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Entities.Concrete;

public class Announcement : BaseEntity
{
    public int CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    public AnnouncementAudienceType AudienceType { get; set; }
    public string? TargetRole { get; set; }
    public string? TargetUserIds { get; set; }
    public string Channels { get; set; } = string.Empty;

    public AnnouncementStatus Status { get; set; } = AnnouncementStatus.Scheduled;
    public DateTime? ScheduledAt { get; set; }
    public DateTime? SentAt { get; set; }

    public int RecipientCount { get; set; }
    public int DeliveredCount { get; set; }
    public int FailedCount { get; set; }
}
