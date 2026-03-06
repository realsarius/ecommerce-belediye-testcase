using EcommerceAPI.Business.Concrete;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Application.Abstractions.Persistence;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;
using FluentAssertions;
using Moq;

namespace EcommerceAPI.UnitTests;

public class NotificationPreferenceManagerTests
{
    private readonly Mock<INotificationPreferenceDal> _notificationPreferenceDalMock;
    private readonly Mock<INotificationTemplateSettingDal> _notificationTemplateSettingDalMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly NotificationPreferenceManager _manager;

    public NotificationPreferenceManagerTests()
    {
        _notificationPreferenceDalMock = new Mock<INotificationPreferenceDal>();
        _notificationTemplateSettingDalMock = new Mock<INotificationTemplateSettingDal>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _manager = new NotificationPreferenceManager(
            _notificationPreferenceDalMock.Object,
            _notificationTemplateSettingDalMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task GetUserPreferencesAsync_WhenUserHasNoSavedPreferences_ShouldReturnTemplateDefaults()
    {
        _notificationPreferenceDalMock
            .Setup(x => x.GetByUserIdAsync(42))
            .ReturnsAsync([]);
        _notificationTemplateSettingDalMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync([]);

        var result = await _manager.GetUserPreferencesAsync(42);

        result.Success.Should().BeTrue();
        result.Data.Preferences.Should().Contain(x =>
            x.Type == "Wishlist" &&
            x.InAppEnabled &&
            x.EmailEnabled &&
            !x.PushEnabled);
        result.Data.Templates.Should().Contain(x =>
            x.Type == "Campaign" &&
            x.SupportsInApp &&
            !x.SupportsEmail);
    }

    [Fact]
    public async Task UpdateUserPreferencesAsync_ShouldClampUnsupportedChannelsToTemplateCapabilities()
    {
        _notificationPreferenceDalMock
            .Setup(x => x.GetByUserIdAsync(42))
            .ReturnsAsync([]);
        _notificationTemplateSettingDalMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync([]);

        NotificationPreference? addedPreference = null;
        _notificationPreferenceDalMock
            .Setup(x => x.AddAsync(It.IsAny<NotificationPreference>()))
            .Callback<NotificationPreference>(preference => addedPreference = preference)
            .ReturnsAsync((NotificationPreference preference) => preference);

        var request = new UpdateNotificationPreferencesRequest
        {
            Preferences =
            [
                new NotificationPreferenceUpdateItemDto
                {
                    Type = "Campaign",
                    InAppEnabled = true,
                    EmailEnabled = true,
                    PushEnabled = true
                }
            ]
        };

        var result = await _manager.UpdateUserPreferencesAsync(42, request);

        result.Success.Should().BeTrue();
        addedPreference.Should().NotBeNull();
        addedPreference!.InAppEnabled.Should().BeTrue();
        addedPreference.EmailEnabled.Should().BeFalse();
        addedPreference.PushEnabled.Should().BeTrue();
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetChannelSettingsAsync_ShouldReturnSavedPreferenceWhenItExists()
    {
        _notificationPreferenceDalMock
            .Setup(x => x.GetByUserIdsAndTypeAsync(It.IsAny<IEnumerable<int>>(), NotificationType.Refund))
            .ReturnsAsync(
            [
                new NotificationPreference
                {
                    UserId = 42,
                    Type = NotificationType.Refund,
                    InAppEnabled = false,
                    EmailEnabled = true,
                    PushEnabled = true
                }
            ]);

        var result = await _manager.GetChannelSettingsAsync(42, NotificationType.Refund);

        result.InAppEnabled.Should().BeFalse();
        result.EmailEnabled.Should().BeTrue();
        result.PushEnabled.Should().BeTrue();
        result.SupportsEmail.Should().BeTrue();
    }

    [Fact]
    public async Task GetTemplatesAsync_WhenOverrideExists_ShouldReturnMergedTemplateContent()
    {
        _notificationTemplateSettingDalMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(
            [
                new NotificationTemplateSetting
                {
                    Type = NotificationType.Campaign,
                    DisplayName = "Kampanya Merkezi",
                    Description = "Özelleştirilmiş kampanya açıklaması.",
                    TitleExample = "Kampanyada yeni durum",
                    BodyExample = "Takip ettiğiniz kampanyada yeni bir gelişme oluştu."
                }
            ]);

        var result = await _manager.GetTemplatesAsync();

        result.Success.Should().BeTrue();
        result.Data.Should().Contain(x =>
            x.Type == "Campaign" &&
            x.DisplayName == "Kampanya Merkezi" &&
            x.Description == "Özelleştirilmiş kampanya açıklaması." &&
            x.TitleExample == "Kampanyada yeni durum");
    }

    [Fact]
    public async Task UpdateTemplateAsync_ShouldUpsertTemplateOverride()
    {
        _notificationTemplateSettingDalMock
            .Setup(x => x.GetByTypeAsync(NotificationType.Wishlist))
            .ReturnsAsync((NotificationTemplateSetting?)null);

        NotificationTemplateSetting? added = null;
        _notificationTemplateSettingDalMock
            .Setup(x => x.AddAsync(It.IsAny<NotificationTemplateSetting>()))
            .Callback<NotificationTemplateSetting>(entity => added = entity)
            .ReturnsAsync((NotificationTemplateSetting entity) => entity);

        var request = new UpdateNotificationTemplateRequest
        {
            DisplayName = "Favori Uyarıları",
            Description = "Yeni açıklama",
            TitleExample = "Yeni başlık",
            BodyExample = "Yeni içerik"
        };

        var result = await _manager.UpdateTemplateAsync("Wishlist", request);

        result.Success.Should().BeTrue();
        added.Should().NotBeNull();
        added!.Type.Should().Be(NotificationType.Wishlist);
        added.DisplayName.Should().Be("Favori Uyarıları");
        added.TitleExample.Should().Be("Yeni başlık");
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }
}
