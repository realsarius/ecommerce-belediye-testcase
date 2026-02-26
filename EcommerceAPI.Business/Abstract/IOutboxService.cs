namespace EcommerceAPI.Business.Abstract;

public interface IOutboxService
{
    Task EnqueueAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class;
}
