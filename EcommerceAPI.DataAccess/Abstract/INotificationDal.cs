using EcommerceAPI.Core.DataAccess;
using EcommerceAPI.Entities.Concrete;

namespace EcommerceAPI.DataAccess.Abstract;

public interface INotificationDal : IEntityRepository<Notification>
{
    Task<IList<Notification>> GetUserNotificationsAsync(int userId, int take = 50);
    Task<int> CountUnreadAsync(int userId);
    Task<Notification?> GetByIdForUserAsync(int id, int userId);
    Task<int> MarkAllAsReadAsync(int userId, DateTime readAtUtc);
}
