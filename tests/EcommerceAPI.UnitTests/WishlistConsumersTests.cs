using EcommerceAPI.API.Consumers;
using EcommerceAPI.Business.Abstract;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.IntegrationEvents;
using FluentAssertions;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;

namespace EcommerceAPI.UnitTests;

public class WishlistConsumersTests
{
    [Fact]
    public async Task WishlistAnalyticsConsumer_WhenAddedEventConsumed_SavesInboxMessage()
    {
        await using var dbContext = CreateDbContext();
        var category = new Category { Id = 10, Name = "Elektronik", Description = "Elektronik" };
        var product = new Product
        {
            Id = 25,
            Name = "Kulaklik",
            Description = "Test urunu",
            Price = 499m,
            CategoryId = category.Id,
            Category = category,
            WishlistCount = 7,
            IsActive = true,
            SKU = "SKU-25"
        };

        dbContext.Categories.Add(category);
        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();

        var consumer = new WishlistAnalyticsConsumer(
            dbContext,
            Mock.Of<ILogger<WishlistAnalyticsConsumer>>());

        var message = new WishlistItemAddedEvent
        {
            EventId = Guid.NewGuid(),
            UserId = 42,
            WishlistId = 8,
            ProductId = product.Id,
            PriceAtTime = product.Price,
            Currency = product.Currency
        };

        var context = CreateConsumeContext(message);

        await consumer.Consume(context.Object);

        dbContext.InboxMessages.Should().ContainSingle(x =>
            x.ConsumerName == "WishlistAnalyticsConsumer" &&
            x.MessageId == message.EventId);
    }

    [Fact]
    public async Task WishlistProductIndexSyncConsumer_WhenDuplicateMessageReceived_SkipsIndexing()
    {
        await using var dbContext = CreateDbContext();
        var messageId = Guid.NewGuid();

        dbContext.InboxMessages.Add(new InboxMessage
        {
            ConsumerName = "WishlistProductIndexSyncConsumer",
            MessageId = messageId,
            MessageType = typeof(WishlistItemAddedEvent).FullName ?? nameof(WishlistItemAddedEvent),
            ProcessedOnUtc = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var productSearchIndexService = new Mock<IProductSearchIndexService>();
        var consumer = new WishlistProductIndexSyncConsumer(
            dbContext,
            productSearchIndexService.Object,
            Mock.Of<ILogger<WishlistProductIndexSyncConsumer>>());

        var message = new WishlistItemAddedEvent
        {
            EventId = messageId,
            UserId = 42,
            WishlistId = 8,
            ProductId = 99,
            PriceAtTime = 125m
        };

        var context = CreateConsumeContext(message, messageId);

        await consumer.Consume(context.Object);

        productSearchIndexService.Verify(
            x => x.IndexProductAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WishlistPersonalizationConsumer_WhenAddedEventConsumed_UpdatesRedisAndInbox()
    {
        await using var dbContext = CreateDbContext();
        var category = new Category { Id = 15, Name = "Kitap", Description = "Kitap" };
        var product = new Product
        {
            Id = 31,
            Name = "Roman",
            Description = "Test urunu",
            Price = 120m,
            CategoryId = category.Id,
            Category = category,
            IsActive = true,
            SKU = "SKU-31"
        };

        dbContext.Categories.Add(category);
        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();

        var databaseMock = new Mock<IDatabase>();
        databaseMock
            .Setup(x => x.HashIncrementAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<long>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(1L);

        var redisMock = new Mock<IConnectionMultiplexer>();
        redisMock
            .Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(databaseMock.Object);

        var consumer = new WishlistPersonalizationConsumer(
            dbContext,
            redisMock.Object,
            Mock.Of<ILogger<WishlistPersonalizationConsumer>>());

        var message = new WishlistItemAddedEvent
        {
            EventId = Guid.NewGuid(),
            UserId = 77,
            WishlistId = 12,
            ProductId = product.Id,
            PriceAtTime = product.Price
        };

        var context = CreateConsumeContext(message);

        await consumer.Consume(context.Object);

        databaseMock.Verify(
            x => x.HashIncrementAsync(
                "wishlist:preferences:user:77",
                category.Id.ToString(),
                1,
                It.IsAny<CommandFlags>()),
            Times.Once);

        dbContext.InboxMessages.Should().ContainSingle(x =>
            x.ConsumerName == "WishlistPersonalizationConsumer" &&
            x.MessageId == message.EventId);
    }

    [Fact]
    public async Task WishlistLowStockNotificationConsumer_WhenEventConsumed_NotifiesWishlistUsersAndSavesInbox()
    {
        await using var dbContext = CreateDbContext();
        var user = new User
        {
            Id = 42,
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "hash",
            EmailHash = "email-hash",
            RoleId = 1
        };
        var wishlist = new Wishlist { Id = 8, UserId = user.Id, User = user };
        var collection = new WishlistCollection
        {
            Id = 16,
            WishlistId = wishlist.Id,
            Wishlist = wishlist,
            Name = "Favorilerim",
            IsDefault = true
        };
        var product = new Product
        {
            Id = 99,
            Name = "Mekanik Klavye",
            Description = "Test urunu",
            Price = 999m,
            CategoryId = 1,
            IsActive = true,
            SKU = "SKU-99"
        };
        var wishlistItem = new WishlistItem
        {
            Id = 1,
            WishlistId = wishlist.Id,
            Wishlist = wishlist,
            ProductId = product.Id,
            CollectionId = collection.Id,
            Collection = collection,
            AddedAtPrice = 999m,
            AddedAt = DateTime.UtcNow
        };

        dbContext.Users.Add(user);
        dbContext.Wishlists.Add(wishlist);
        dbContext.WishlistCollections.Add(collection);
        dbContext.Products.Add(product);
        dbContext.WishlistItems.Add(wishlistItem);
        await dbContext.SaveChangesAsync();

        var clientProxy = new Mock<IClientProxy>();
        var hubClients = new Mock<IHubClients>();
        hubClients
            .Setup(x => x.Group("wishlist-user-42"))
            .Returns(clientProxy.Object);

        var hubContext = new Mock<IHubContext<EcommerceAPI.API.Hubs.WishlistHub>>();
        hubContext.SetupGet(x => x.Clients).Returns(hubClients.Object);

        var consumer = new WishlistLowStockNotificationConsumer(
            dbContext,
            hubContext.Object,
            Mock.Of<ILogger<WishlistLowStockNotificationConsumer>>());

        var message = new WishlistProductLowStockEvent
        {
            EventId = Guid.NewGuid(),
            ProductId = product.Id,
            StockQuantity = 3,
            Threshold = 5,
            Reason = "Order Reservation"
        };

        var context = CreateConsumeContext(message);

        await consumer.Consume(context.Object);

        clientProxy.Verify(
            x => x.SendCoreAsync(
                "LowStockAlertTriggered",
                It.Is<object?[]>(args => args.Length == 1),
                It.IsAny<CancellationToken>()),
            Times.Once);

        dbContext.InboxMessages.Should().ContainSingle(x =>
            x.ConsumerName == "WishlistLowStockNotificationConsumer" &&
            x.MessageId == message.EventId);
    }

    private static AppDbContext CreateDbContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        Microsoft.EntityFrameworkCore.InMemoryDbContextOptionsExtensions.UseInMemoryDatabase(
            optionsBuilder,
            Guid.NewGuid().ToString("N"));
        var options = optionsBuilder.Options;

        return new AppDbContext(options);
    }

    private static Mock<ConsumeContext<TMessage>> CreateConsumeContext<TMessage>(
        TMessage message,
        Guid? messageId = null)
        where TMessage : class
    {
        var context = new Mock<ConsumeContext<TMessage>>();
        context.SetupGet(x => x.Message).Returns(message);
        context.SetupGet(x => x.MessageId).Returns(messageId ?? ResolveMessageId(message));
        context.SetupGet(x => x.CancellationToken).Returns(CancellationToken.None);
        return context;
    }

    private static Guid ResolveMessageId<TMessage>(TMessage message)
        where TMessage : class
    {
        return message switch
        {
            WishlistItemAddedEvent added => added.EventId,
            WishlistItemRemovedEvent removed => removed.EventId,
            WishlistProductLowStockEvent lowStock => lowStock.EventId,
            _ => Guid.NewGuid()
        };
    }
}
