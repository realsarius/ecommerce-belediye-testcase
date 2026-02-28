using MassTransit;

namespace EcommerceAPI.API.Consumers;

public sealed class WishlistPriceAlertNotificationConsumerDefinition : ConsumerDefinition<WishlistPriceAlertNotificationConsumer>
{
    public WishlistPriceAlertNotificationConsumerDefinition()
    {
        EndpointName = "wishlist-price-alert-notifications";
        ConcurrentMessageLimit = 4;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<WishlistPriceAlertNotificationConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(retry =>
        {
            retry.Interval(3, TimeSpan.FromSeconds(2));
        });

        endpointConfigurator.UseInMemoryOutbox(context);
    }
}
