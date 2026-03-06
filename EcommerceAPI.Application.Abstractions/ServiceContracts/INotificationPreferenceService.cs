using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Application.Abstractions.ServiceContracts;

public interface INotificationPreferenceService
{
    Task<IDataResult<NotificationPreferencesResponseDto>> GetUserPreferencesAsync(int userId);
    Task<IDataResult<List<NotificationPreferenceDto>>> UpdateUserPreferencesAsync(int userId, UpdateNotificationPreferencesRequest request);
    Task<IDataResult<List<NotificationTemplateDto>>> GetTemplatesAsync();
    Task<IDataResult<NotificationTemplateDto>> UpdateTemplateAsync(string type, UpdateNotificationTemplateRequest request);
    Task<NotificationChannelSettingsDto> GetChannelSettingsAsync(int userId, NotificationType type);
    Task<Dictionary<int, NotificationChannelSettingsDto>> GetChannelSettingsByUsersAsync(IEnumerable<int> userIds, NotificationType type);
}
