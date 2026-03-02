namespace EcommerceAPI.Entities.DTOs;

public class NotificationPreferencesResponseDto
{
    public List<NotificationPreferenceDto> Preferences { get; set; } = new();
    public List<NotificationTemplateDto> Templates { get; set; } = new();
}
