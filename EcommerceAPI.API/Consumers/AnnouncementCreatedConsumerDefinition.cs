using MassTransit;

namespace EcommerceAPI.API.Consumers;

public sealed class AnnouncementCreatedConsumerDefinition : ConsumerDefinition<AnnouncementCreatedConsumer>
{
    public AnnouncementCreatedConsumerDefinition()
    {
        EndpointName = "announcement-created";
        ConcurrentMessageLimit = 2;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<AnnouncementCreatedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(retry => retry.Interval(3, TimeSpan.FromSeconds(5)));
    }
}
