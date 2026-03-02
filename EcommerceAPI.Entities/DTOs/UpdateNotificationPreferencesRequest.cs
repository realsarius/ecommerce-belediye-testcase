namespace EcommerceAPI.Entities.DTOs;

public class UpdateNotificationPreferencesRequest
{
    public List<NotificationPreferenceUpdateItemDto> Preferences { get; set; } = new();
}

public class NotificationPreferenceUpdateItemDto
{
    public string Type { get; set; } = string.Empty;
    public bool InAppEnabled { get; set; }
    public bool EmailEnabled { get; set; }
    public bool PushEnabled { get; set; }
}
