using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Entities.Concrete;

public class NotificationTemplateSetting : BaseEntity
{
    public NotificationType Type { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TitleExample { get; set; } = string.Empty;
    public string BodyExample { get; set; } = string.Empty;
}
