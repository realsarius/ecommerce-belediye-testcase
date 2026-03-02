namespace EcommerceAPI.Entities.DTOs;

public class NotificationTemplateDto
{
    public string Type { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string TitleExample { get; set; } = string.Empty;
    public string BodyExample { get; set; } = string.Empty;
    public bool SupportsInApp { get; set; }
    public bool SupportsEmail { get; set; }
    public bool SupportsPush { get; set; }
}
