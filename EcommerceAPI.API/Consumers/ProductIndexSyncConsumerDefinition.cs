using MassTransit;

namespace EcommerceAPI.API.Consumers;

public sealed class ProductIndexSyncConsumerDefinition : ConsumerDefinition<ProductIndexSyncConsumer>
{
    public ProductIndexSyncConsumerDefinition()
    {
        EndpointName = "product-index-sync";
        ConcurrentMessageLimit = 4;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<ProductIndexSyncConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(retry =>
        {
            retry.Interval(3, TimeSpan.FromSeconds(2));
        });

        endpointConfigurator.UseInMemoryOutbox(context);
    }
}
