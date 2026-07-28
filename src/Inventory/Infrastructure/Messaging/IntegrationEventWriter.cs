using System.Text.Json;
using Application;
using Infrastructure.Outbox;
using MessageContract;

using Application.Abstracts;
namespace Infrastructure.Messaging;

public sealed class IntegrationEventWriter(IOutboxStore store) : IIntegrationEventWriter
{
    public async Task WriteAsync(IIntegrationEvent @event, CancellationToken ct)
    {
        var payload = JsonSerializer.Serialize(@event, @event.GetType());
        var message = OutboxMessage.Create(@event.Id, @event.GetType().Name, payload);

        await store.AddAsync(message, ct);
    }
}
