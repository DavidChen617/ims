using MessageContract;

namespace Application.Abstracts;

public interface IIntegrationEventWriter
{
    Task WriteAsync(IIntegrationEvent @event, CancellationToken ct);
}
