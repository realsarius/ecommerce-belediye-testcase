using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Business.Abstract;

public interface INotificationPreferenceService
{
    Task<IDataResult<NotificationPreferencesResponseDto>> GetUserPreferencesAsync(int userId);
    Task<IDataResult<List<NotificationPreferenceDto>>> UpdateUserPreferencesAsync(int userId, UpdateNotificationPreferencesRequest request);
    Task<IDataResult<List<NotificationTemplateDto>>> GetTemplatesAsync();
    Task<IDataResult<NotificationTemplateDto>> UpdateTemplateAsync(string type, UpdateNotificationTemplateRequest request);
    Task<NotificationChannelSettingsDto> GetChannelSettingsAsync(int userId, NotificationType type);
    Task<Dictionary<int, NotificationChannelSettingsDto>> GetChannelSettingsAsync(IEnumerable<int> userIds, NotificationType type);
}
