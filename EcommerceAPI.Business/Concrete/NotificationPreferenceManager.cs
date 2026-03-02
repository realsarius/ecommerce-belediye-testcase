using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Business.Concrete;

public class NotificationPreferenceManager : INotificationPreferenceService
{
    private static readonly NotificationTemplateDefinition[] TemplateDefinitions =
    [
        new(
            NotificationType.Wishlist,
            "Wishlist",
            "Wishlist Bildirimleri",
            "Fiyat alarmları, düşük stok ve wishlist tabanlı fırsat güncellemeleri.",
            "{Ürün Adı} için önemli wishlist güncellemesi",
            "Favorilerinizdeki ürünle ilgili yeni bir gelişme var.",
            SupportsInApp: true,
            SupportsEmail: true,
            SupportsPush: true,
            DefaultInApp: true,
            DefaultEmail: true,
            DefaultPush: false),
        new(
            NotificationType.Refund,
            "Refund",
            "İade ve Refund",
            "İade talebi, refund tamamlanması veya hata durumları.",
            "İade işleminizle ilgili güncelleme",
            "İade sürecinizde yeni bir durum oluştu.",
            SupportsInApp: true,
            SupportsEmail: true,
            SupportsPush: true,
            DefaultInApp: true,
            DefaultEmail: true,
            DefaultPush: false),
        new(
            NotificationType.Campaign,
            "Campaign",
            "Kampanya Bildirimleri",
            "Takip ettiğiniz kampanyaların bitişi ve önemli lifecycle değişimleri.",
            "{Kampanya Adı} kampanyasında yeni durum",
            "Takip ettiğiniz kampanya için yeni bir güncelleme var.",
            SupportsInApp: true,
            SupportsEmail: false,
            SupportsPush: true,
            DefaultInApp: true,
            DefaultEmail: false,
            DefaultPush: false),
        new(
            NotificationType.Order,
            "Order",
            "Sipariş Güncellemeleri",
            "Siparişinizin kritik durum değişimleri ve teslimat aşamaları.",
            "Siparişiniz güncellendi",
            "Sipariş yaşam döngünüzde yeni bir gelişme var.",
            SupportsInApp: true,
            SupportsEmail: true,
            SupportsPush: true,
            DefaultInApp: true,
            DefaultEmail: true,
            DefaultPush: false),
        new(
            NotificationType.Support,
            "Support",
            "Destek Mesajları",
            "Canlı destek ve destek talepleriyle ilgili önemli bildirimler.",
            "Destek talebiniz güncellendi",
            "Destek ekibimizden yeni bir yanıt var.",
            SupportsInApp: true,
            SupportsEmail: true,
            SupportsPush: true,
            DefaultInApp: true,
            DefaultEmail: true,
            DefaultPush: false)
    ];

    private readonly INotificationPreferenceDal _notificationPreferenceDal;
    private readonly IUnitOfWork _unitOfWork;

    public NotificationPreferenceManager(
        INotificationPreferenceDal notificationPreferenceDal,
        IUnitOfWork unitOfWork)
    {
        _notificationPreferenceDal = notificationPreferenceDal;
        _unitOfWork = unitOfWork;
    }

    public async Task<IDataResult<NotificationPreferencesResponseDto>> GetUserPreferencesAsync(int userId)
    {
        if (userId <= 0)
        {
            return new ErrorDataResult<NotificationPreferencesResponseDto>("Geçersiz kullanıcı.");
        }

        var existingPreferences = await _notificationPreferenceDal.GetByUserIdAsync(userId);
        var response = new NotificationPreferencesResponseDto
        {
            Preferences = BuildPreferenceDtos(existingPreferences),
            Templates = BuildTemplateDtos()
        };

        return new SuccessDataResult<NotificationPreferencesResponseDto>(response);
    }

    public async Task<IDataResult<List<NotificationPreferenceDto>>> UpdateUserPreferencesAsync(int userId, UpdateNotificationPreferencesRequest request)
    {
        if (userId <= 0)
        {
            return new ErrorDataResult<List<NotificationPreferenceDto>>("Geçersiz kullanıcı.");
        }

        if (request.Preferences.Count == 0)
        {
            return new ErrorDataResult<List<NotificationPreferenceDto>>("En az bir tercih gönderilmelidir.");
        }

        var existingByType = (await _notificationPreferenceDal.GetByUserIdAsync(userId))
            .ToDictionary(x => x.Type);

        foreach (var item in request.Preferences)
        {
            var template = FindTemplate(item.Type);
            if (template == null)
            {
                return new ErrorDataResult<List<NotificationPreferenceDto>>($"Geçersiz bildirim tipi: {item.Type}");
            }

            var normalized = new NotificationPreference
            {
                UserId = userId,
                Type = template.Type,
                InAppEnabled = template.SupportsInApp && item.InAppEnabled,
                EmailEnabled = template.SupportsEmail && item.EmailEnabled,
                PushEnabled = template.SupportsPush && item.PushEnabled
            };

            if (existingByType.TryGetValue(template.Type, out var existing))
            {
                existing.InAppEnabled = normalized.InAppEnabled;
                existing.EmailEnabled = normalized.EmailEnabled;
                existing.PushEnabled = normalized.PushEnabled;
                existing.UpdatedAt = DateTime.UtcNow;
                _notificationPreferenceDal.Update(existing);
            }
            else
            {
                normalized.CreatedAt = DateTime.UtcNow;
                normalized.UpdatedAt = normalized.CreatedAt;
                await _notificationPreferenceDal.AddAsync(normalized);
            }
        }

        await _unitOfWork.SaveChangesAsync();

        var updated = await _notificationPreferenceDal.GetByUserIdAsync(userId);
        return new SuccessDataResult<List<NotificationPreferenceDto>>(BuildPreferenceDtos(updated));
    }

    public async Task<NotificationChannelSettingsDto> GetChannelSettingsAsync(int userId, NotificationType type)
    {
        var preferences = await GetChannelSettingsAsync([userId], type);
        return preferences.TryGetValue(userId, out var settings)
            ? settings
            : BuildDefaultChannelSettings(type);
    }

    public async Task<Dictionary<int, NotificationChannelSettingsDto>> GetChannelSettingsAsync(IEnumerable<int> userIds, NotificationType type)
    {
        var ids = userIds.Where(id => id > 0).Distinct().ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        var template = FindTemplate(type) ?? throw new InvalidOperationException($"Template tanımı bulunamadı: {type}");
        var existing = await _notificationPreferenceDal.GetByUserIdsAndTypeAsync(ids, type);
        var existingByUserId = existing.ToDictionary(x => x.UserId);

        return ids.ToDictionary(
            id => id,
            id => existingByUserId.TryGetValue(id, out var preference)
                ? MapToChannelSettings(template, preference)
                : MapToDefaultChannelSettings(template));
    }

    private static List<NotificationPreferenceDto> BuildPreferenceDtos(IEnumerable<NotificationPreference> existingPreferences)
    {
        var existingByType = existingPreferences.ToDictionary(x => x.Type);

        return TemplateDefinitions
            .Select(template =>
            {
                var dto = MapToPreferenceDto(template);
                if (existingByType.TryGetValue(template.Type, out var preference))
                {
                    dto.InAppEnabled = template.SupportsInApp && preference.InAppEnabled;
                    dto.EmailEnabled = template.SupportsEmail && preference.EmailEnabled;
                    dto.PushEnabled = template.SupportsPush && preference.PushEnabled;
                }

                return dto;
            })
            .ToList();
    }

    private static List<NotificationTemplateDto> BuildTemplateDtos()
    {
        return TemplateDefinitions
            .Select(template => new NotificationTemplateDto
            {
                Type = template.TypeName,
                DisplayName = template.DisplayName,
                TitleExample = template.TitleExample,
                BodyExample = template.BodyExample,
                SupportsInApp = template.SupportsInApp,
                SupportsEmail = template.SupportsEmail,
                SupportsPush = template.SupportsPush
            })
            .ToList();
    }

    private static NotificationPreferenceDto MapToPreferenceDto(NotificationTemplateDefinition template)
    {
        return new NotificationPreferenceDto
        {
            Type = template.TypeName,
            DisplayName = template.DisplayName,
            Description = template.Description,
            InAppEnabled = template.DefaultInApp,
            EmailEnabled = template.DefaultEmail,
            PushEnabled = template.DefaultPush,
            SupportsInApp = template.SupportsInApp,
            SupportsEmail = template.SupportsEmail,
            SupportsPush = template.SupportsPush
        };
    }

    private static NotificationTemplateDefinition? FindTemplate(string typeName)
    {
        return TemplateDefinitions.FirstOrDefault(
            x => x.TypeName.Equals(typeName, StringComparison.OrdinalIgnoreCase));
    }

    private static NotificationTemplateDefinition? FindTemplate(NotificationType type)
    {
        return TemplateDefinitions.FirstOrDefault(x => x.Type == type);
    }

    private static NotificationChannelSettingsDto BuildDefaultChannelSettings(NotificationType type)
    {
        var template = FindTemplate(type) ?? throw new InvalidOperationException($"Template tanımı bulunamadı: {type}");
        return MapToDefaultChannelSettings(template);
    }

    private static NotificationChannelSettingsDto MapToDefaultChannelSettings(NotificationTemplateDefinition template)
    {
        return new NotificationChannelSettingsDto
        {
            InAppEnabled = template.DefaultInApp,
            EmailEnabled = template.DefaultEmail,
            PushEnabled = template.DefaultPush,
            SupportsInApp = template.SupportsInApp,
            SupportsEmail = template.SupportsEmail,
            SupportsPush = template.SupportsPush
        };
    }

    private static NotificationChannelSettingsDto MapToChannelSettings(
        NotificationTemplateDefinition template,
        NotificationPreference preference)
    {
        return new NotificationChannelSettingsDto
        {
            InAppEnabled = template.SupportsInApp && preference.InAppEnabled,
            EmailEnabled = template.SupportsEmail && preference.EmailEnabled,
            PushEnabled = template.SupportsPush && preference.PushEnabled,
            SupportsInApp = template.SupportsInApp,
            SupportsEmail = template.SupportsEmail,
            SupportsPush = template.SupportsPush
        };
    }

    private sealed record NotificationTemplateDefinition(
        NotificationType Type,
        string TypeName,
        string DisplayName,
        string Description,
        string TitleExample,
        string BodyExample,
        bool SupportsInApp,
        bool SupportsEmail,
        bool SupportsPush,
        bool DefaultInApp,
        bool DefaultEmail,
        bool DefaultPush);
}
