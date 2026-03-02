namespace EcommerceAPI.Entities.DTOs;

public class NotificationChannelSettingsDto
{
    public bool InAppEnabled { get; set; }
    public bool EmailEnabled { get; set; }
    public bool PushEnabled { get; set; }
    public bool SupportsInApp { get; set; }
    public bool SupportsEmail { get; set; }
    public bool SupportsPush { get; set; }
}
