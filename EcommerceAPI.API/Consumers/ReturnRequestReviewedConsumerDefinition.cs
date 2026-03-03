using MassTransit;

namespace EcommerceAPI.API.Consumers;

public sealed class ReturnRequestReviewedConsumerDefinition : ConsumerDefinition<ReturnRequestReviewedConsumer>
{
    public ReturnRequestReviewedConsumerDefinition()
    {
        EndpointName = "return-request-reviewed";
        ConcurrentMessageLimit = 2;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<ReturnRequestReviewedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(retry => retry.Interval(3, TimeSpan.FromSeconds(5)));
    }
}
