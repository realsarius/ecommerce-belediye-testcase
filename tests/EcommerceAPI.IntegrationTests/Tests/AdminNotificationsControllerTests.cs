using System.Net;
using System.Net.Http.Json;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.IntegrationTests.Utilities;
using FluentAssertions;

namespace EcommerceAPI.IntegrationTests.Tests;

[Collection("Integration")]
public class AdminNotificationsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AdminNotificationsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetTemplates_AsAdmin_ShouldReturnNotificationTemplates()
    {
        var client = _factory.CreateClient().AsAdmin(1);

        var response = await client.GetAsync("/api/v1/admin/notifications/templates");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResult<List<NotificationTemplateDto>>>();
        result.Should().NotBeNull();
        result!.Data.Should().Contain(x => x.Type == "Wishlist");
    }

    [Fact]
    public async Task UpdateTemplate_AsAdmin_ShouldPersistTemplateOverride()
    {
        var client = _factory.CreateClient().AsAdmin(1);

        var response = await client.PutAsJsonAsync("/api/v1/admin/notifications/templates/Campaign", new UpdateNotificationTemplateRequest
        {
            DisplayName = "Kampanya Merkezi",
            Description = "Yönetilen kampanya bildirimleri",
            TitleExample = "Kampanya güncellendi",
            BodyExample = "Takip ettiğiniz kampanya için yeni durum oluştu."
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResult<NotificationTemplateDto>>();
        result.Should().NotBeNull();
        result!.Data.DisplayName.Should().Be("Kampanya Merkezi");
        result.Data.Description.Should().Be("Yönetilen kampanya bildirimleri");

        var preferencesClient = _factory.CreateClient().AsCustomer(991001);
        var preferencesResponse = await preferencesClient.GetAsync("/api/v1/notifications/preferences");
        preferencesResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var preferences = await preferencesResponse.Content.ReadFromJsonAsync<ApiResult<NotificationPreferencesResponseDto>>();
        preferences.Should().NotBeNull();
        preferences!.Data.Templates.Should().Contain(x =>
            x.Type == "Campaign" &&
            x.DisplayName == "Kampanya Merkezi" &&
            x.Description == "Yönetilen kampanya bildirimleri");
    }
}
