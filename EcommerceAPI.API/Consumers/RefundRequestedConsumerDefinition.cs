using MassTransit;

namespace EcommerceAPI.API.Consumers;

public sealed class RefundRequestedConsumerDefinition : ConsumerDefinition<RefundRequestedConsumer>
{
    public RefundRequestedConsumerDefinition()
    {
        EndpointName = "refund-requested";
        ConcurrentMessageLimit = 2;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<RefundRequestedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(retry =>
        {
            retry.Interval(3, TimeSpan.FromSeconds(5));
        });

        endpointConfigurator.UseInMemoryOutbox(context);
    }
}
