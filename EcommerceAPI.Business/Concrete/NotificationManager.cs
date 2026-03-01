using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;
using Microsoft.Extensions.Logging;

namespace EcommerceAPI.Business.Concrete;

public class NotificationManager : INotificationService
{
    private readonly INotificationDal _notificationDal;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<NotificationManager> _logger;

    public NotificationManager(
        INotificationDal notificationDal,
        IUnitOfWork unitOfWork,
        ILogger<NotificationManager> logger)
    {
        _notificationDal = notificationDal;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IDataResult<List<NotificationDto>>> GetUserNotificationsAsync(int userId, int take = 50)
    {
        var notifications = await _notificationDal.GetUserNotificationsAsync(userId, Math.Clamp(take, 1, 100));
        return new SuccessDataResult<List<NotificationDto>>(notifications.Select(MapToDto).ToList());
    }

    public async Task<IDataResult<NotificationCountDto>> GetUnreadCountAsync(int userId)
    {
        var unreadCount = await _notificationDal.CountUnreadAsync(userId);
        return new SuccessDataResult<NotificationCountDto>(new NotificationCountDto
        {
            UnreadCount = unreadCount
        });
    }

    public async Task<IDataResult<NotificationDto>> MarkAsReadAsync(int userId, int notificationId)
    {
        var notification = await _notificationDal.GetByIdForUserAsync(notificationId, userId);
        if (notification == null)
        {
            return new ErrorDataResult<NotificationDto>("Bildirim bulunamadı.");
        }

        if (!notification.IsRead)
        {
            var now = DateTime.UtcNow;
            notification.IsRead = true;
            notification.ReadAt = now;
            notification.UpdatedAt = now;
            _notificationDal.Update(notification);
            await _unitOfWork.SaveChangesAsync();
        }

        return new SuccessDataResult<NotificationDto>(MapToDto(notification));
    }

    public async Task<IResult> MarkAllAsReadAsync(int userId)
    {
        await _notificationDal.MarkAllAsReadAsync(userId, DateTime.UtcNow);
        return new SuccessResult("Bildirimlerin tamamı okundu olarak işaretlendi.");
    }

    public async Task<IDataResult<NotificationDto>> CreateNotificationAsync(CreateNotificationRequest request)
    {
        if (request.UserId <= 0)
        {
            return new ErrorDataResult<NotificationDto>("Geçersiz kullanıcı.");
        }

        if (!Enum.TryParse<NotificationType>(request.Type, true, out var parsedType))
        {
            return new ErrorDataResult<NotificationDto>("Geçersiz bildirim tipi.");
        }

        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Body))
        {
            return new ErrorDataResult<NotificationDto>("Bildirim başlığı ve içeriği zorunludur.");
        }

        var now = DateTime.UtcNow;
        var notification = new Notification
        {
            UserId = request.UserId,
            Type = parsedType,
            Title = request.Title.Trim(),
            Body = request.Body.Trim(),
            DeepLink = string.IsNullOrWhiteSpace(request.DeepLink) ? null : request.DeepLink.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };

        await _notificationDal.AddAsync(notification);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "Notification created. UserId={UserId}, Type={Type}, NotificationId={NotificationId}, DeepLink={DeepLink}",
            notification.UserId,
            notification.Type,
            notification.Id,
            notification.DeepLink);

        return new SuccessDataResult<NotificationDto>(MapToDto(notification));
    }

    private static NotificationDto MapToDto(Notification notification)
    {
        return new NotificationDto
        {
            Id = notification.Id,
            UserId = notification.UserId,
            Type = notification.Type.ToString(),
            Title = notification.Title,
            Body = notification.Body,
            DeepLink = notification.DeepLink,
            IsRead = notification.IsRead,
            ReadAt = notification.ReadAt,
            CreatedAt = notification.CreatedAt
        };
    }
}
