namespace Application;

public interface IInboxStore
{
    Task<bool> HasProcessedAsync(Guid eventId, CancellationToken ct);
    Task MarkProcessedAsync(Guid eventId, CancellationToken ct);
}
