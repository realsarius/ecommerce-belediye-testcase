using System.Security.Claims;
using EcommerceAPI.API.Hubs;
using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Entities.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Http.Features;
using Moq;
using StackExchange.Redis;

namespace EcommerceAPI.UnitTests;

public class LiveSupportHubTests
{
    [Fact]
    public async Task JoinConversation_WhenAccessDenied_ThrowsHubException()
    {
        var supportService = new Mock<ISupportConversationService>();
        supportService
            .Setup(x => x.CanAccessConversationAsync(15, 42, "Customer"))
            .ReturnsAsync(false);

        var hub = CreateHub(
            supportService.Object,
            CreateRedis(1),
            CreateContext(userId: 42, role: "Customer"));

        var act = () => hub.JoinConversation(15);

        await act.Should().ThrowAsync<HubException>()
            .WithMessage("Bu görüşmeye erişim yetkiniz yok");
    }

    [Fact]
    public async Task JoinConversation_WhenAccessAllowed_AddsGroup_AndNotifiesCaller()
    {
        var supportService = new Mock<ISupportConversationService>();
        supportService
            .Setup(x => x.CanAccessConversationAsync(25, 77, "Customer"))
            .ReturnsAsync(true);

        var groups = new Mock<IGroupManager>();
        var caller = new Mock<ISingleClientProxy>();
        var clients = new Mock<IHubCallerClients>();

        clients.SetupGet(x => x.Caller).Returns(caller.Object);

        var hub = CreateHub(
            supportService.Object,
            CreateRedis(1),
            CreateContext(userId: 77, role: "Customer"),
            clients.Object,
            groups.Object);

        await hub.JoinConversation(25);

        groups.Verify(x => x.AddToGroupAsync(It.IsAny<string>(), "support-conv-25", default), Times.Once);
        caller.Verify(
            x => x.SendCoreAsync(
                "JoinedConversation",
                It.Is<object?[]>(args => args.Length == 1 && (int)args[0]! == 25),
                default),
            Times.Once);
    }

    [Fact]
    public async Task SendMessage_WhenRateLimitExceeded_ThrowsHubException()
    {
        var supportService = new Mock<ISupportConversationService>();

        var hub = CreateHub(
            supportService.Object,
            CreateRedis(21),
            CreateContext(userId: 55, role: "Customer"));

        var act = () => hub.SendMessage(5, "Test mesaj");

        await act.Should().ThrowAsync<HubException>()
            .WithMessage("RATE_LIMIT_EXCEEDED");

        supportService.Verify(
            x => x.SendMessageAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<SendSupportMessageRequest>()),
            Times.Never);
    }

    [Fact]
    public async Task SendMessage_WhenSuccessful_PublishesMessageToConversationGroup()
    {
        var expectedMessage = new SupportMessageDto
        {
            Id = 11,
            ConversationId = 9,
            SenderUserId = 88,
            SenderRole = "Support",
            SenderName = "Support User",
            Message = "Size yardımcı oluyorum.",
            IsSystemMessage = false,
            CreatedAt = DateTime.UtcNow
        };

        var supportService = new Mock<ISupportConversationService>();
        supportService
            .Setup(x => x.SendMessageAsync(
                9,
                88,
                "Support",
                It.Is<SendSupportMessageRequest>(r => r.Message == "Size yardımcı oluyorum.")))
            .ReturnsAsync(new SuccessDataResult<SupportMessageDto>(expectedMessage));

        var groupClient = new Mock<IClientProxy>();
        var clients = new Mock<IHubCallerClients>();
        clients.Setup(x => x.Group("support-conv-9")).Returns(groupClient.Object);

        var hub = CreateHub(
            supportService.Object,
            CreateRedis(1),
            CreateContext(userId: 88, role: "Support"),
            clients.Object);

        await hub.SendMessage(9, "Size yardımcı oluyorum.");

        groupClient.Verify(
            x => x.SendCoreAsync(
                "ReceiveMessage",
                It.Is<object?[]>(args => args.Length == 1 && ReferenceEquals(args[0], expectedMessage)),
                default),
            Times.Once);
    }

    private static LiveSupportHub CreateHub(
        ISupportConversationService supportService,
        IConnectionMultiplexer redis,
        HubCallerContext context,
        IHubCallerClients? clients = null,
        IGroupManager? groups = null)
    {
        return new LiveSupportHub(supportService, redis)
        {
            Context = context,
            Clients = clients ?? Mock.Of<IHubCallerClients>(),
            Groups = groups ?? Mock.Of<IGroupManager>()
        };
    }

    private static IConnectionMultiplexer CreateRedis(long incrementValue)
    {
        var database = new Mock<IDatabase>();
        database
            .Setup(x => x.StringIncrementAsync(It.IsAny<RedisKey>(), It.IsAny<long>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(incrementValue);
        database
            .Setup(x => x.KeyExpireAsync(It.IsAny<RedisKey>(), It.IsAny<TimeSpan?>(), It.IsAny<ExpireWhen>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        var redis = new Mock<IConnectionMultiplexer>();
        redis.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(database.Object);
        return redis.Object;
    }

    private static HubCallerContext CreateContext(int userId, string role)
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role)
        ], "Test"));

        return new TestHubCallerContext(user);
    }

    private sealed class TestHubCallerContext : HubCallerContext
    {
        private readonly ClaimsPrincipal _user;
        private readonly IDictionary<object, object?> _items;

        public TestHubCallerContext(ClaimsPrincipal user)
        {
            _user = user;
            _items = new Dictionary<object, object?>();
        }

        public override string ConnectionId => "test-connection";
        public override string? UserIdentifier => _user.FindFirstValue(ClaimTypes.NameIdentifier);
        public override ClaimsPrincipal? User => _user;
        public override IDictionary<object, object?> Items => _items;
        public override IFeatureCollection Features => new FeatureCollection();
        public override CancellationToken ConnectionAborted => CancellationToken.None;
        public override void Abort()
        {
        }
    }
}
