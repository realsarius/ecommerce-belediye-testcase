using MassTransit;

namespace EcommerceAPI.API.Consumers;

public sealed class ContactMessageReceivedConsumerDefinition : ConsumerDefinition<ContactMessageReceivedConsumer>
{
    public ContactMessageReceivedConsumerDefinition()
    {
        EndpointName = "contact-message-received";
        ConcurrentMessageLimit = 4;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<ContactMessageReceivedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(r => r.Interval(2, TimeSpan.FromSeconds(10)));
    }
}
