using MassTransit;

namespace EcommerceAPI.API.Consumers;

public sealed class WishlistAnalyticsConsumerDefinition : ConsumerDefinition<WishlistAnalyticsConsumer>
{
    public WishlistAnalyticsConsumerDefinition()
    {
        EndpointName = "wishlist-analytics";
        ConcurrentMessageLimit = 4;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<WishlistAnalyticsConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(retry =>
        {
            retry.Interval(3, TimeSpan.FromSeconds(2));
        });

        endpointConfigurator.UseInMemoryOutbox(context);
    }
}
