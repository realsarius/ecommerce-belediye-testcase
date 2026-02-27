using System.Net;
using System.Net.Http.Json;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.IntegrationTests.Utilities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EcommerceAPI.IntegrationTests.Tests;

[Collection("Integration")]
public class SupportControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SupportControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task StartConversation_AsCustomer_ReturnsOk()
    {
        var customerId = Random.Shared.Next(700_001, 710_000);
        await EnsureUserAsync(customerId, "Customer");

        var client = _factory.CreateClient().AsCustomer(customerId);
        var request = new StartSupportConversationRequest
        {
            Subject = $"Destek Talebi {Guid.NewGuid():N}",
            InitialMessage = "Siparişimle ilgili destek almak istiyorum."
        };

        var response = await client.PostAsJsonAsync("/api/v1/support/conversations", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResult<SupportConversationDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.CustomerUserId.Should().Be(customerId);
        result.Data.Subject.Should().Be(request.Subject);
        result.Data.Status.Should().Be("Open");
    }

    [Fact]
    public async Task GetQueue_AsCustomer_Returns403()
    {
        var client = _factory.CreateClient().AsCustomer(Random.Shared.Next(710_001, 720_000));

        var response = await client.GetAsync("/api/v1/support/conversations/queue");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AssignConversation_AsSupport_ToAnotherSupportUser_ReturnsBadRequest()
    {
        var customerId = Random.Shared.Next(720_001, 730_000);
        var actingSupportId = Random.Shared.Next(730_001, 740_000);
        var otherSupportId = Random.Shared.Next(740_001, 750_000);

        await EnsureUserAsync(customerId, "Customer");
        await EnsureUserAsync(actingSupportId, "Support");
        await EnsureUserAsync(otherSupportId, "Support");

        var customerClient = _factory.CreateClient().AsCustomer(customerId);
        var createResponse = await customerClient.PostAsJsonAsync(
            "/api/v1/support/conversations",
            new StartSupportConversationRequest
            {
                Subject = $"Atama Testi {Guid.NewGuid():N}",
                InitialMessage = "Temsilci ataması test ediliyor."
            });

        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var created = await createResponse.Content.ReadFromJsonAsync<ApiResult<SupportConversationDto>>();
        created.Should().NotBeNull();

        var supportClient = _factory.CreateClient().AsUser(actingSupportId, "Support");
        var assignResponse = await supportClient.PostAsJsonAsync(
            $"/api/v1/support/conversations/{created!.Data.Id}/assign",
            new AssignSupportConversationRequest { SupportUserId = otherSupportId });

        assignResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var assignResult = await assignResponse.Content.ReadFromJsonAsync<ApiResult<SupportConversationDto>>();
        assignResult.Should().NotBeNull();
        assignResult!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task SupportConversation_FullFlow_CustomerAndSupport_CanExchangeMessages_AndCloseConversation()
    {
        var customerId = Random.Shared.Next(750_001, 760_000);
        var supportId = Random.Shared.Next(760_001, 770_000);

        await EnsureUserAsync(customerId, "Customer");
        await EnsureUserAsync(supportId, "Support");

        var customerClient = _factory.CreateClient().AsCustomer(customerId);
        var supportClient = _factory.CreateClient().AsUser(supportId, "Support");

        var createResponse = await customerClient.PostAsJsonAsync(
            "/api/v1/support/conversations",
            new StartSupportConversationRequest
            {
                Subject = $"Full Flow {Guid.NewGuid():N}",
                InitialMessage = "Merhaba, siparişimi takip edemiyorum."
            });

        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var created = await createResponse.Content.ReadFromJsonAsync<ApiResult<SupportConversationDto>>();
        created.Should().NotBeNull();
        var conversationId = created!.Data.Id;

        var queueResponse = await supportClient.GetAsync("/api/v1/support/conversations/queue?page=1&pageSize=20");
        queueResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var queueResult = await queueResponse.Content.ReadFromJsonAsync<ApiResult<PaginatedResponse<SupportConversationDto>>>();
        queueResult.Should().NotBeNull();
        queueResult!.Success.Should().BeTrue();
        queueResult.Data.Items.Should().Contain(x => x.Id == conversationId);

        var sendSupportMessageResponse = await supportClient.PostAsJsonAsync(
            $"/api/v1/support/conversations/{conversationId}/messages",
            new SendSupportMessageRequest { Message = "Merhaba, size yardımcı oluyorum." });

        sendSupportMessageResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var messagesResponse = await customerClient.GetAsync($"/api/v1/support/conversations/{conversationId}/messages?page=1&pageSize=20");
        messagesResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var messagesResult = await messagesResponse.Content.ReadFromJsonAsync<ApiResult<PaginatedResponse<SupportMessageDto>>>();
        messagesResult.Should().NotBeNull();
        messagesResult!.Success.Should().BeTrue();
        messagesResult.Data.Items.Should().Contain(x => x.Message == "Merhaba, siparişimi takip edemiyorum.");
        messagesResult.Data.Items.Should().Contain(x => x.Message == "Merhaba, size yardımcı oluyorum.");

        var closeResponse = await supportClient.PostAsync($"/api/v1/support/conversations/{conversationId}/close", content: null);
        closeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var closeResult = await closeResponse.Content.ReadFromJsonAsync<ApiResult<SupportConversationDto>>();
        closeResult.Should().NotBeNull();
        closeResult!.Success.Should().BeTrue();
        closeResult.Data.Status.Should().Be("Closed");

        var myConversationsResponse = await customerClient.GetAsync("/api/v1/support/conversations/my");
        myConversationsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var myConversations = await myConversationsResponse.Content.ReadFromJsonAsync<ApiResult<List<SupportConversationDto>>>();
        myConversations.Should().NotBeNull();
        myConversations!.Success.Should().BeTrue();
        myConversations.Data.Should().Contain(x => x.Id == conversationId && x.Status == "Closed");
    }

    [Fact]
    public async Task GetMessages_WithoutAccess_ReturnsBadRequest()
    {
        var customerId = Random.Shared.Next(770_001, 780_000);
        var otherCustomerId = Random.Shared.Next(780_001, 790_000);

        await EnsureUserAsync(customerId, "Customer");
        await EnsureUserAsync(otherCustomerId, "Customer");

        var conversationId = await CreateConversationAsync(
            customerId,
            $"Erişim Testi {Guid.NewGuid():N}",
            "Bu görüşmeye sadece sahibi erişebilmeli.");

        var otherCustomerClient = _factory.CreateClient().AsCustomer(otherCustomerId);
        var response = await otherCustomerClient.GetAsync($"/api/v1/support/conversations/{conversationId}/messages?page=1&pageSize=20");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<ApiResult<PaginatedResponse<SupportMessageDto>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task CloseConversation_AsDifferentCustomer_ReturnsBadRequest()
    {
        var customerId = Random.Shared.Next(790_001, 800_000);
        var otherCustomerId = Random.Shared.Next(800_001, 810_000);

        await EnsureUserAsync(customerId, "Customer");
        await EnsureUserAsync(otherCustomerId, "Customer");

        var conversationId = await CreateConversationAsync(
            customerId,
            $"Close Auth {Guid.NewGuid():N}",
            "Yanlış kullanıcı kapatmayı deneyecek.");

        var otherCustomerClient = _factory.CreateClient().AsCustomer(otherCustomerId);
        var response = await otherCustomerClient.PostAsync($"/api/v1/support/conversations/{conversationId}/close", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<ApiResult<SupportConversationDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task AssignConversation_AsAdmin_AssignsSuccessfully()
    {
        var customerId = Random.Shared.Next(810_001, 820_000);
        var supportId = Random.Shared.Next(820_001, 830_000);
        var adminId = Random.Shared.Next(830_001, 840_000);

        await EnsureUserAsync(customerId, "Customer");
        await EnsureUserAsync(supportId, "Support");
        await EnsureUserAsync(adminId, "Admin");

        var conversationId = await CreateConversationAsync(
            customerId,
            $"Admin Assign {Guid.NewGuid():N}",
            "Admin atama akışı test ediliyor.");

        var adminClient = _factory.CreateClient().AsAdmin(adminId);
        var response = await adminClient.PostAsJsonAsync(
            $"/api/v1/support/conversations/{conversationId}/assign",
            new AssignSupportConversationRequest { SupportUserId = supportId });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResult<SupportConversationDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.SupportUserId.Should().Be(supportId);
        result.Data.Status.Should().Be("Assigned");
    }

    [Fact]
    public async Task GetMyConversations_AsSupport_ReturnsAssignedItems()
    {
        var customerId = Random.Shared.Next(840_001, 850_000);
        var supportId = Random.Shared.Next(850_001, 860_000);
        var adminId = Random.Shared.Next(860_001, 870_000);

        await EnsureUserAsync(customerId, "Customer");
        await EnsureUserAsync(supportId, "Support");
        await EnsureUserAsync(adminId, "Admin");

        var conversationId = await CreateConversationAsync(
            customerId,
            $"Support List {Guid.NewGuid():N}",
            "Atanmış görüşme listede görünmeli.");

        var adminClient = _factory.CreateClient().AsAdmin(adminId);
        var assignResponse = await adminClient.PostAsJsonAsync(
            $"/api/v1/support/conversations/{conversationId}/assign",
            new AssignSupportConversationRequest { SupportUserId = supportId });

        assignResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var supportClient = _factory.CreateClient().AsUser(supportId, "Support");
        var response = await supportClient.GetAsync("/api/v1/support/conversations/my");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResult<List<SupportConversationDto>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().Contain(x => x.Id == conversationId && x.SupportUserId == supportId);
    }

    [Fact]
    public async Task SendMessage_WithEmptyMessage_ReturnsBadRequest()
    {
        var customerId = Random.Shared.Next(870_001, 880_000);

        await EnsureUserAsync(customerId, "Customer");

        var conversationId = await CreateConversationAsync(
            customerId,
            $"Empty Message {Guid.NewGuid():N}",
            "İlk mesaj");

        var customerClient = _factory.CreateClient().AsCustomer(customerId);
        var response = await customerClient.PostAsJsonAsync(
            $"/api/v1/support/conversations/{conversationId}/messages",
            new SendSupportMessageRequest { Message = "   " });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<ApiResult<SupportMessageDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task GetMessages_AfterClose_IncludesSystemCloseMessage()
    {
        var customerId = Random.Shared.Next(880_001, 890_000);
        var supportId = Random.Shared.Next(890_001, 900_000);

        await EnsureUserAsync(customerId, "Customer");
        await EnsureUserAsync(supportId, "Support");

        var conversationId = await CreateConversationAsync(
            customerId,
            $"Close Message {Guid.NewGuid():N}",
            "Kapatma sonrası sistem mesajı bekleniyor.");

        var supportClient = _factory.CreateClient().AsUser(supportId, "Support");
        var closeResponse = await supportClient.PostAsync($"/api/v1/support/conversations/{conversationId}/close", content: null);

        closeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var customerClient = _factory.CreateClient().AsCustomer(customerId);
        var messagesResponse = await customerClient.GetAsync($"/api/v1/support/conversations/{conversationId}/messages?page=1&pageSize=20");

        messagesResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await messagesResponse.Content.ReadFromJsonAsync<ApiResult<PaginatedResponse<SupportMessageDto>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Items.Should().Contain(x => x.IsSystemMessage && x.Message == "Görüşme kapatıldı.");
    }

    private async Task EnsureUserAsync(int userId, string role)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await TestDataSeeder.EnsureUserAsync(db, userId, role);
    }

    private async Task<int> CreateConversationAsync(int customerId, string subject, string initialMessage)
    {
        var client = _factory.CreateClient().AsCustomer(customerId);
        var response = await client.PostAsJsonAsync(
            "/api/v1/support/conversations",
            new StartSupportConversationRequest
            {
                Subject = subject,
                InitialMessage = initialMessage
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResult<SupportConversationDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();

        return result.Data.Id;
    }
}
