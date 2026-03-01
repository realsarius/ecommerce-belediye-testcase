using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Entities.DTOs;

namespace EcommerceAPI.Business.Abstract;

public interface INotificationService
{
    Task<IDataResult<List<NotificationDto>>> GetUserNotificationsAsync(int userId, int take = 50);
    Task<IDataResult<NotificationCountDto>> GetUnreadCountAsync(int userId);
    Task<IDataResult<NotificationDto>> MarkAsReadAsync(int userId, int notificationId);
    Task<IResult> MarkAllAsReadAsync(int userId);
    Task<IDataResult<NotificationDto>> CreateNotificationAsync(CreateNotificationRequest request);
}
