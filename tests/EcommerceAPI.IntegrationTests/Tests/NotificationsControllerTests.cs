using System.Net;
using System.Net.Http.Json;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;
using EcommerceAPI.IntegrationTests.Utilities;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace EcommerceAPI.IntegrationTests.Tests;

[Collection("Integration")]
public class NotificationsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public NotificationsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetUnreadCount_ShouldReturnOnlyUnreadNotifications()
    {
        var userId = Random.Shared.Next(910_001, 911_000);
        await SeedNotificationsAsync(userId);

        var client = _factory.CreateClient().AsCustomer(userId);
        var response = await client.GetAsync("/api/v1/notifications/unread-count");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResult<NotificationCountDto>>();
        result.Should().NotBeNull();
        result!.Data.UnreadCount.Should().Be(2);
    }

    [Fact]
    public async Task MarkAsRead_ShouldUpdateNotificationState()
    {
        var userId = Random.Shared.Next(911_001, 912_000);
        var notificationId = await SeedNotificationsAsync(userId);

        var client = _factory.CreateClient().AsCustomer(userId);
        var response = await client.PostAsync($"/api/v1/notifications/{notificationId}/read", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var notification = await db.Notifications.FindAsync(notificationId);
        notification.Should().NotBeNull();
        notification!.IsRead.Should().BeTrue();
        notification.ReadAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPreferences_ShouldReturnTemplatesAndEffectiveDefaults()
    {
        var userId = Random.Shared.Next(912_001, 913_000);
        await EnsureUserAsync(userId);
        var client = _factory.CreateClient().AsCustomer(userId);

        var response = await client.GetAsync("/api/v1/notifications/preferences");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResult<NotificationPreferencesResponseDto>>();
        result.Should().NotBeNull();
        result!.Data.Preferences.Should().Contain(x =>
            x.Type == "Wishlist" &&
            x.InAppEnabled &&
            x.EmailEnabled);
        result.Data.Templates.Should().Contain(x =>
            x.Type == "Campaign" &&
            !x.SupportsEmail);
    }

    [Fact]
    public async Task UpdatePreferences_ShouldPersistChanges()
    {
        var userId = Random.Shared.Next(913_001, 914_000);
        await EnsureUserAsync(userId);
        var client = _factory.CreateClient().AsCustomer(userId);

        var response = await client.PutAsJsonAsync("/api/v1/notifications/preferences", new UpdateNotificationPreferencesRequest
        {
            Preferences =
            [
                new NotificationPreferenceUpdateItemDto
                {
                    Type = "Campaign",
                    InAppEnabled = true,
                    EmailEnabled = true,
                    PushEnabled = true
                },
                new NotificationPreferenceUpdateItemDto
                {
                    Type = "Refund",
                    InAppEnabled = false,
                    EmailEnabled = true,
                    PushEnabled = false
                }
            ]
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResult<List<NotificationPreferenceDto>>>();
        result.Should().NotBeNull();
        result!.Data.Should().Contain(x =>
            x.Type == "Campaign" &&
            x.InAppEnabled &&
            !x.EmailEnabled &&
            x.PushEnabled);
        result.Data.Should().Contain(x =>
            x.Type == "Refund" &&
            !x.InAppEnabled &&
            x.EmailEnabled &&
            !x.PushEnabled);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var preferences = db.NotificationPreferences.Where(x => x.UserId == userId).ToList();
        preferences.Should().Contain(x =>
            x.Type == NotificationType.Campaign &&
            x.InAppEnabled &&
            !x.EmailEnabled &&
            x.PushEnabled);
        preferences.Should().Contain(x =>
            x.Type == NotificationType.Refund &&
            !x.InAppEnabled &&
            x.EmailEnabled &&
            !x.PushEnabled);
    }

    private async Task<int> SeedNotificationsAsync(int userId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await TestDataSeeder.EnsureUserAsync(db, userId);

        var first = new Notification
        {
            UserId = userId,
            Type = NotificationType.Wishlist,
            Title = "Fiyat düştü",
            Body = "Wishlist ürününüzde fiyat düşüşü oldu.",
            DeepLink = "/products/1",
            IsRead = false
        };

        var second = new Notification
        {
            UserId = userId,
            Type = NotificationType.Refund,
            Title = "İade güncellendi",
            Body = "İade talebiniz işleme alındı.",
            DeepLink = "/returns",
            IsRead = false
        };

        var third = new Notification
        {
            UserId = userId,
            Type = NotificationType.Refund,
            Title = "Eski bildirim",
            Body = "Bu bildirim zaten okundu.",
            DeepLink = "/returns",
            IsRead = true,
            ReadAt = DateTime.UtcNow
        };

        db.Notifications.AddRange(first, second, third);
        await db.SaveChangesAsync();

        return first.Id;
    }

    private async Task EnsureUserAsync(int userId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await TestDataSeeder.EnsureUserAsync(db, userId);
    }
}
