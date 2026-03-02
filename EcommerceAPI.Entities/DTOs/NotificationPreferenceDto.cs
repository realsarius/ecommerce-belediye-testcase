namespace EcommerceAPI.Entities.DTOs;

public class NotificationPreferenceDto
{
    public string Type { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool InAppEnabled { get; set; }
    public bool EmailEnabled { get; set; }
    public bool PushEnabled { get; set; }
    public bool SupportsInApp { get; set; }
    public bool SupportsEmail { get; set; }
    public bool SupportsPush { get; set; }
}
