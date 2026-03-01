using EcommerceAPI.API.Consumers;
using EcommerceAPI.API.Hubs;
using EcommerceAPI.Business.Abstract;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;
using EcommerceAPI.Entities.IntegrationEvents;
using FluentAssertions;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace EcommerceAPI.UnitTests;

public class CampaignConsumerTests
{
    [Fact]
    public async Task CampaignStatusChangedConsumer_WhenCampaignEnds_ShouldNotifyInterestedUsersAndSaveInbox()
    {
        await using var dbContext = CreateDbContext();

        var user = new User
        {
            Id = 81,
            Email = "campaign@test.com",
            FirstName = "Campaign",
            LastName = "User",
            PasswordHash = "hash",
            EmailHash = "hash",
            RoleId = 1
        };
        var campaign = new Campaign
        {
            Id = 91,
            Name = "Gece Kampanyası",
            Status = CampaignStatus.Ended,
            IsEnabled = true,
            StartsAt = DateTime.UtcNow.AddHours(-3),
            EndsAt = DateTime.UtcNow.AddMinutes(-5)
        };
        var product = new Product
        {
            Id = 92,
            Name = "Klavye",
            Description = "d",
            Price = 500,
            CategoryId = 1,
            SKU = "SKU-92",
            IsActive = true,
            CampaignProducts =
            [
                new CampaignProduct
                {
                    CampaignId = campaign.Id,
                    Campaign = campaign,
                    CampaignPrice = 450,
                    OriginalPriceSnapshot = 500
                }
            ]
        };
        var wishlist = new Wishlist { Id = 93, UserId = user.Id, User = user };
        var collection = new WishlistCollection
        {
            Id = 94,
            WishlistId = wishlist.Id,
            Wishlist = wishlist,
            Name = "Favorilerim",
            IsDefault = true
        };
        var wishlistItem = new WishlistItem
        {
            Id = 95,
            WishlistId = wishlist.Id,
            Wishlist = wishlist,
            ProductId = product.Id,
            Product = product,
            CollectionId = collection.Id,
            Collection = collection,
            AddedAtPrice = 500,
            AddedAt = DateTime.UtcNow
        };

        dbContext.Users.Add(user);
        dbContext.Campaigns.Add(campaign);
        dbContext.Products.Add(product);
        dbContext.Wishlists.Add(wishlist);
        dbContext.WishlistCollections.Add(collection);
        dbContext.WishlistItems.Add(wishlistItem);
        await dbContext.SaveChangesAsync();

        var clientProxy = new Mock<IClientProxy>();
        var hubClients = new Mock<IHubClients>();
        hubClients
            .Setup(x => x.Group("wishlist-user-81"))
            .Returns(clientProxy.Object);

        var hubContext = new Mock<IHubContext<WishlistHub>>();
        hubContext.SetupGet(x => x.Clients).Returns(hubClients.Object);

        var notificationService = new Mock<INotificationService>();
        notificationService
            .Setup(x => x.CreateNotificationAsync(It.IsAny<CreateNotificationRequest>()))
            .ReturnsAsync(new EcommerceAPI.Core.Utilities.Results.SuccessDataResult<NotificationDto>(new NotificationDto()));

        var consumer = new CampaignStatusChangedConsumer(
            dbContext,
            hubContext.Object,
            notificationService.Object,
            Mock.Of<ILogger<CampaignStatusChangedConsumer>>());

        var message = new CampaignStatusChangedEvent
        {
            EventId = Guid.NewGuid(),
            CampaignId = campaign.Id,
            CampaignName = campaign.Name,
            PreviousStatus = CampaignStatus.Active,
            CurrentStatus = CampaignStatus.Ended,
            StartsAt = campaign.StartsAt,
            EndsAt = campaign.EndsAt,
            ProductCount = 1,
            OccurredAt = DateTime.UtcNow
        };

        var context = CreateConsumeContext(message);
        await consumer.Consume(context.Object);

        notificationService.Verify(
            x => x.CreateNotificationAsync(It.Is<CreateNotificationRequest>(request =>
                request.UserId == user.Id &&
                request.Type == "Campaign")),
            Times.Once);

        clientProxy.Verify(
            x => x.SendCoreAsync(
                "CampaignStatusChanged",
                It.Is<object?[]>(args => args.Length == 1),
                It.IsAny<CancellationToken>()),
            Times.Once);

        dbContext.InboxMessages.Should().ContainSingle(x =>
            x.ConsumerName == "CampaignStatusChangedConsumer" &&
            x.MessageId == message.EventId);
    }

    private static AppDbContext CreateDbContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        Microsoft.EntityFrameworkCore.InMemoryDbContextOptionsExtensions.UseInMemoryDatabase(
            optionsBuilder,
            Guid.NewGuid().ToString("N"));
        return new AppDbContext(optionsBuilder.Options);
    }

    private static Mock<ConsumeContext<TMessage>> CreateConsumeContext<TMessage>(TMessage message)
        where TMessage : class
    {
        var context = new Mock<ConsumeContext<TMessage>>();
        context.SetupGet(x => x.Message).Returns(message);
        context.SetupGet(x => x.MessageId).Returns(message switch
        {
            CampaignStatusChangedEvent campaignStatusChanged => campaignStatusChanged.EventId,
            _ => Guid.NewGuid()
        });
        context.SetupGet(x => x.CancellationToken).Returns(CancellationToken.None);
        return context;
    }
}
