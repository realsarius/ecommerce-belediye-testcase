using MassTransit;

namespace EcommerceAPI.API.Consumers;

public sealed class OrderStatusChangedConsumerDefinition : ConsumerDefinition<OrderStatusChangedConsumer>
{
    public OrderStatusChangedConsumerDefinition()
    {
        EndpointName = "order-status-changed";
        ConcurrentMessageLimit = 4;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<OrderStatusChangedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(retry =>
        {
            retry.Interval(3, TimeSpan.FromSeconds(2));
        });

        endpointConfigurator.UseInMemoryOutbox(context);
    }
}
