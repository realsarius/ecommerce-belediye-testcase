using EcommerceAPI.Business.Concrete;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.IntegrationEvents;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;

namespace EcommerceAPI.UnitTests;

public class ContactMessageManagerTests
{
    [Fact]
    public async Task CreateAsync_ShouldPersistContactMessage_AndPublishEvent()
    {
        var contactDal = new Mock<IContactMessageDal>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var publishEndpoint = new Mock<IPublishEndpoint>();

        contactDal
            .Setup(x => x.AddAsync(It.IsAny<ContactMessage>()))
            .Callback<ContactMessage>(message => message.Id = 77)
            .ReturnsAsync((ContactMessage message) => message);

        var manager = new ContactMessageManager(
            contactDal.Object,
            unitOfWork.Object,
            publishEndpoint.Object,
            Mock.Of<ILogger<ContactMessageManager>>());

        var result = await manager.CreateAsync(
            new CreateContactMessageRequest
            {
                Name = "Berkan Sözer",
                Email = "berkan@test.com",
                Subject = "Sipariş sorusu",
                Message = "Siparişimle ilgili kısa bir bilgi almak istiyorum."
            },
            "127.0.0.1",
            "test-agent");

        result.Success.Should().BeTrue();
        result.Data.Id.Should().Be(77);
        publishEndpoint.Verify(
            x => x.Publish(
                It.Is<ContactMessageReceivedEvent>(evt =>
                    evt.ContactMessageId == 77 &&
                    evt.Email == "berkan@test.com" &&
                    evt.Subject == "Sipariş sorusu"),
                It.IsAny<CancellationToken>()),
            Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(), Times.Exactly(2));
    }
}
