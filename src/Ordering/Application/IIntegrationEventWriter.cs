using MessageContract;

namespace Application;

public interface IIntegrationEventWriter
{
    Task WriteAsync(IIntegrationEvent @event, CancellationToken ct);
}
