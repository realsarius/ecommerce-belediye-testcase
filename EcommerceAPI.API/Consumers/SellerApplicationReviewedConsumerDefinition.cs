using MassTransit;

namespace EcommerceAPI.API.Consumers;

public sealed class SellerApplicationReviewedConsumerDefinition : ConsumerDefinition<SellerApplicationReviewedConsumer>
{
    public SellerApplicationReviewedConsumerDefinition()
    {
        EndpointName = "seller-application-reviewed";
        ConcurrentMessageLimit = 2;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<SellerApplicationReviewedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(retry => retry.Interval(3, TimeSpan.FromSeconds(5)));
    }
}
