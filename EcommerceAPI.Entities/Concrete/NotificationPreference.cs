using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Entities.Concrete;

public class NotificationPreference : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public NotificationType Type { get; set; }

    public bool InAppEnabled { get; set; } = true;
    public bool EmailEnabled { get; set; } = true;
    public bool PushEnabled { get; set; }
}
