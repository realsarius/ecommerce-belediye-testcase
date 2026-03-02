namespace EcommerceAPI.Entities.DTOs;

public class UpdateNotificationTemplateRequest
{
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TitleExample { get; set; } = string.Empty;
    public string BodyExample { get; set; } = string.Empty;
}
