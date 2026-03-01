using MassTransit;

namespace EcommerceAPI.API.Consumers;

public sealed class CampaignStatusChangedConsumerDefinition : ConsumerDefinition<CampaignStatusChangedConsumer>
{
    public CampaignStatusChangedConsumerDefinition()
    {
        EndpointName = "campaign-status-changed";
        ConcurrentMessageLimit = 4;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<CampaignStatusChangedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(retry =>
        {
            retry.Interval(3, TimeSpan.FromSeconds(2));
        });

        endpointConfigurator.UseInMemoryOutbox(context);
    }
}
