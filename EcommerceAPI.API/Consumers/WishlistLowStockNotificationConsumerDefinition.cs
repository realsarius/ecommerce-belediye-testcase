using MassTransit;

namespace EcommerceAPI.API.Consumers;

public sealed class WishlistLowStockNotificationConsumerDefinition : ConsumerDefinition<WishlistLowStockNotificationConsumer>
{
    public WishlistLowStockNotificationConsumerDefinition()
    {
        EndpointName = "wishlist-low-stock-notifications";
        ConcurrentMessageLimit = 4;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<WishlistLowStockNotificationConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(retry =>
        {
            retry.Interval(3, TimeSpan.FromSeconds(2));
        });

        endpointConfigurator.UseInMemoryOutbox(context);
    }
}
