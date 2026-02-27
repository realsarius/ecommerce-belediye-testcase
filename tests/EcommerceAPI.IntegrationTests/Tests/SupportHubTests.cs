using System.Net;
using System.Net.Http.Json;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.IntegrationTests.Utilities;
using FluentAssertions;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace EcommerceAPI.IntegrationTests.Tests;

[Collection("Integration")]
public class SupportHubTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SupportHubTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task JoinConversation_WhenCustomerOwnsConversation_ReceivesJoinedConversationEvent()
    {
        var customerId = Random.Shared.Next(900_001, 910_000);
        await EnsureUserAsync(customerId, "Customer");

        var conversationId = await CreateConversationAsync(
            customerId,
            $"Hub Join {Guid.NewGuid():N}",
            "Kendi görüşmeme bağlanıyorum.");

        await using var connection = CreateHubConnection(customerId, "Customer");
        var joinedTcs = CreateCompletionSource<int>();

        connection.On<int>("JoinedConversation", conversationIdFromEvent =>
        {
            joinedTcs.TrySetResult(conversationIdFromEvent);
        });

        await connection.StartAsync();
        await connection.InvokeAsync("JoinConversation", conversationId);

        var joinedConversationId = await WaitAsync(joinedTcs.Task);
        joinedConversationId.Should().Be(conversationId);
    }

    [Fact]
    public async Task JoinConversation_WhenCustomerDoesNotOwnConversation_ThrowsHubException()
    {
        var ownerCustomerId = Random.Shared.Next(910_001, 920_000);
        var otherCustomerId = Random.Shared.Next(920_001, 930_000);

        await EnsureUserAsync(ownerCustomerId, "Customer");
        await EnsureUserAsync(otherCustomerId, "Customer");

        var conversationId = await CreateConversationAsync(
            ownerCustomerId,
            $"Hub Unauthorized {Guid.NewGuid():N}",
            "Bu görüşmeye başka kullanıcı girmemeli.");

        await using var connection = CreateHubConnection(otherCustomerId, "Customer");
        await connection.StartAsync();

        var act = () => connection.InvokeAsync("JoinConversation", conversationId);

        var exception = await act.Should().ThrowAsync<HubException>();
        exception.Which.Message.Should().Contain("Bu görüşmeye erişim yetkiniz yok");
    }

    [Fact]
    public async Task SendMessage_WhenSupportSends_CustomerReceivesReceiveMessageEvent()
    {
        var customerId = Random.Shared.Next(930_001, 940_000);
        var supportId = Random.Shared.Next(940_001, 950_000);

        await EnsureUserAsync(customerId, "Customer");
        await EnsureUserAsync(supportId, "Support");

        var conversationId = await CreateConversationAsync(
            customerId,
            $"Hub Message {Guid.NewGuid():N}",
            "İlk müşteri mesajı.");

        await using var customerConnection = CreateHubConnection(customerId, "Customer");
        await using var supportConnection = CreateHubConnection(supportId, "Support");

        var receiveMessageTcs = CreateCompletionSource<SupportMessageDto>();
        customerConnection.On<SupportMessageDto>("ReceiveMessage", message =>
        {
            if (message.Message == "Merhaba, destekten bağlanıyorum.")
            {
                receiveMessageTcs.TrySetResult(message);
            }
        });

        await customerConnection.StartAsync();
        await supportConnection.StartAsync();

        await customerConnection.InvokeAsync("JoinConversation", conversationId);
        await supportConnection.InvokeAsync("JoinConversation", conversationId);

        await supportConnection.InvokeAsync("SendMessage", conversationId, "Merhaba, destekten bağlanıyorum.");

        var receivedMessage = await WaitAsync(receiveMessageTcs.Task);
        receivedMessage.ConversationId.Should().Be(conversationId);
        receivedMessage.Message.Should().Be("Merhaba, destekten bağlanıyorum.");
        receivedMessage.SenderRole.Should().Be("Support");
    }

    [Fact]
    public async Task CloseConversation_WhenSupportCloses_CustomerReceivesConversationClosedEvent()
    {
        var customerId = Random.Shared.Next(950_001, 960_000);
        var supportId = Random.Shared.Next(960_001, 970_000);

        await EnsureUserAsync(customerId, "Customer");
        await EnsureUserAsync(supportId, "Support");

        var conversationId = await CreateConversationAsync(
            customerId,
            $"Hub Close {Guid.NewGuid():N}",
            "Görüşme kapanış event'i test ediliyor.");

        await using var customerConnection = CreateHubConnection(customerId, "Customer");
        await using var supportConnection = CreateHubConnection(supportId, "Support");

        var closedTcs = CreateCompletionSource<SupportConversationDto>();
        customerConnection.On<SupportConversationDto>("ConversationClosed", conversation =>
        {
            if (conversation.Id == conversationId)
            {
                closedTcs.TrySetResult(conversation);
            }
        });

        await customerConnection.StartAsync();
        await supportConnection.StartAsync();

        await customerConnection.InvokeAsync("JoinConversation", conversationId);
        await supportConnection.InvokeAsync("JoinConversation", conversationId);

        await supportConnection.InvokeAsync("CloseConversation", conversationId);

        var closedConversation = await WaitAsync(closedTcs.Task);
        closedConversation.Id.Should().Be(conversationId);
        closedConversation.Status.Should().Be("Closed");
    }

    private HubConnection CreateHubConnection(int userId, string role)
    {
        return new HubConnectionBuilder()
            .WithUrl(new Uri(_factory.Server.BaseAddress!, "/hubs/live-support"), options =>
            {
                options.Transports = HttpTransportType.LongPolling;
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                options.Headers["X-Test-UserId"] = userId.ToString();
                options.Headers["X-Test-Role"] = role;
                options.Headers["X-Test-Email"] = $"testuser{userId}@test.com";
            })
            .WithAutomaticReconnect()
            .Build();
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

    private static TaskCompletionSource<T> CreateCompletionSource<T>()
        => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static async Task<T> WaitAsync<T>(Task<T> task, int timeoutSeconds = 5)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        var completed = await Task.WhenAny(task, Task.Delay(Timeout.Infinite, cts.Token));

        if (completed != task)
        {
            throw new TimeoutException($"SignalR event {typeof(T).Name} beklenen sürede gelmedi.");
        }

        return await task;
    }
}
