using EcommerceAPI.Core.DataAccess;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Application.Abstractions.Persistence;

public interface INotificationPreferenceDal : IEntityRepository<NotificationPreference>
{
    Task<IList<NotificationPreference>> GetByUserIdAsync(int userId);
    Task<IList<NotificationPreference>> GetByUserIdsAndTypeAsync(IEnumerable<int> userIds, NotificationType type);
}
