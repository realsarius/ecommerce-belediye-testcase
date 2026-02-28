using MassTransit;

namespace EcommerceAPI.API.Consumers;

public sealed class WishlistPersonalizationConsumerDefinition : ConsumerDefinition<WishlistPersonalizationConsumer>
{
    public WishlistPersonalizationConsumerDefinition()
    {
        EndpointName = "wishlist-personalization";
        ConcurrentMessageLimit = 4;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<WishlistPersonalizationConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(retry =>
        {
            retry.Interval(3, TimeSpan.FromSeconds(2));
        });

        endpointConfigurator.UseInMemoryOutbox(context);
    }
}
