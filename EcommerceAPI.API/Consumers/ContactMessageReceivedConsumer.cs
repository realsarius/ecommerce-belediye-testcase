using EcommerceAPI.Entities.IntegrationEvents;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EcommerceAPI.API.Consumers;

public sealed class ContactMessageReceivedConsumer : IConsumer<ContactMessageReceivedEvent>
{
    private readonly ILogger<ContactMessageReceivedConsumer> _logger;

    public ContactMessageReceivedConsumer(ILogger<ContactMessageReceivedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<ContactMessageReceivedEvent> context)
    {
        _logger.LogInformation(
            "Contact message received event consumed. ContactMessageId={ContactMessageId}, Email={Email}, Subject={Subject}",
            context.Message.ContactMessageId,
            context.Message.Email,
            context.Message.Subject);

        return Task.CompletedTask;
    }
}
