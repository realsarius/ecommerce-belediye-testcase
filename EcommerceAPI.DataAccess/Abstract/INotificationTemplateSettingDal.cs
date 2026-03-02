using EcommerceAPI.Core.DataAccess;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.DataAccess.Abstract;

public interface INotificationTemplateSettingDal : IEntityRepository<NotificationTemplateSetting>
{
    Task<IList<NotificationTemplateSetting>> GetAllAsync();
    Task<NotificationTemplateSetting?> GetByTypeAsync(NotificationType type);
}
