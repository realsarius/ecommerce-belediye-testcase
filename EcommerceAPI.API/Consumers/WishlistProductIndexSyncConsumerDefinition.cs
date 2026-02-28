using MassTransit;

namespace EcommerceAPI.API.Consumers;

public sealed class WishlistProductIndexSyncConsumerDefinition : ConsumerDefinition<WishlistProductIndexSyncConsumer>
{
    public WishlistProductIndexSyncConsumerDefinition()
    {
        EndpointName = "wishlist-product-index-sync";
        ConcurrentMessageLimit = 4;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<WishlistProductIndexSyncConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(retry =>
        {
            retry.Interval(3, TimeSpan.FromSeconds(2));
        });

        endpointConfigurator.UseInMemoryOutbox(context);
    }
}
