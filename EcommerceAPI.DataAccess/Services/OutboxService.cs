using System.Text.Json;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;
using Microsoft.Extensions.Logging;

namespace EcommerceAPI.DataAccess.Services;

public sealed class OutboxService : IOutboxService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly AppDbContext _dbContext;
    private readonly ILogger<OutboxService> _logger;

    public OutboxService(AppDbContext dbContext, ILogger<OutboxService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task EnqueueAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        var eventType = typeof(TEvent).FullName ?? typeof(TEvent).Name;
        var eventId = ResolveEventId(@event);
        var payload = JsonSerializer.Serialize(@event, SerializerOptions);

        _dbContext.OutboxMessages.Add(new OutboxMessage
        {
            EventId = eventId,
            EventType = eventType,
            Payload = payload
        });

        _logger.LogInformation(
            "Outbox message queued. EventType={EventType}, EventId={EventId}",
            eventType,
            eventId);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static Guid ResolveEventId<TEvent>(TEvent @event)
        where TEvent : class
    {
        var prop = typeof(TEvent).GetProperty("EventId");
        if (prop?.PropertyType == typeof(Guid))
        {
            var value = prop.GetValue(@event);
            if (value is Guid guid && guid != Guid.Empty)
            {
                return guid;
            }
        }

        return Guid.NewGuid();
    }
}
