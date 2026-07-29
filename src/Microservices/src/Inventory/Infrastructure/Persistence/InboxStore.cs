using Application;
using Dapper;

namespace Infrastructure.Persistence;

public sealed class InboxStore(IInventoryUnitOfWork unitOfWork) : IInboxStore
{
    public async Task<bool> HasProcessedAsync(Guid eventId, CancellationToken ct)
    {
        var cmd = new CommandDefinition(
            "select exists(select 1 from inbox_messages where event_id = @EventId)",
            new { EventId = eventId },
            cancellationToken: ct,
            transaction: unitOfWork.Transaction
        );

        return await unitOfWork.Connection.ExecuteScalarAsync<bool>(cmd);
    }

    public async Task MarkProcessedAsync(Guid eventId, CancellationToken ct)
    {
        var cmd = new CommandDefinition(
            "insert into inbox_messages(event_id, processed_at) values(@EventId, @ProcessedAt)",
            new { EventId = eventId, ProcessedAt = DateTime.UtcNow },
            cancellationToken: ct,
            transaction: unitOfWork.Transaction
        );

        await unitOfWork.Connection.ExecuteAsync(cmd);
    }
}
