using MassTransit;

namespace EcommerceAPI.API.Consumers;

public sealed class OrderShippedConsumerDefinition : ConsumerDefinition<OrderShippedConsumer>
{
    public OrderShippedConsumerDefinition()
    {
        EndpointName = "order-shipped";
        ConcurrentMessageLimit = 4;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<OrderShippedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(retry =>
        {
            retry.Interval(3, TimeSpan.FromSeconds(2));
        });

        endpointConfigurator.UseInMemoryOutbox(context);
    }
}
