using Dapper;
using Davish.Result;
using Infrastructure.Persistence;

namespace Infrastructure.Outbox;

public interface IOutboxStore
{
    public Task<Result> AddAsync(OutboxMessage message, CancellationToken cancellationToken);
    public Task<List<OutboxMessage>> ListAsync(int batch, CancellationToken cancellationToken);
    public Task<Result> SaveAsync(OutboxMessage message, CancellationToken cancellationToken);
}

public class OutboxStore(IInventoryUnitOfWork unitOfWork) : IOutboxStore
{
    public async Task<Result> AddAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        var cmd = new CommandDefinition(
            """
            insert into outbox_messages(id, event_type, payload, occurred_on)
            values(@Id, @EventType, @Payload, @OccurredOn)
            """,
            message,
            cancellationToken: cancellationToken,
            transaction: unitOfWork.Transaction
        );

        await unitOfWork.Connection.ExecuteAsync(cmd);

        return Result.Success();
    }

    public async Task<List<OutboxMessage>> ListAsync(int batch, CancellationToken cancellationToken)
    {
        var cmd = new CommandDefinition(
            """
            select id, event_type, payload, occurred_on, processed_on, error
            from outbox_messages
            where processed_on is null
            order by occurred_on
            limit @batch
            """,
            new { batch },
            cancellationToken: cancellationToken,
            transaction: unitOfWork.Transaction
        );

        var messages = await unitOfWork.Connection.QueryAsync<OutboxMessage>(cmd);

        return messages.ToList();
    }

    public async Task<Result> SaveAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        var cmd = new CommandDefinition(
            """
            update outbox_messages
            set processed_on = @ProcessedOn, error = @Error
            where id = @Id
            """,
            message,
            cancellationToken: cancellationToken,
            transaction: unitOfWork.Transaction
        );

        await unitOfWork.Connection.ExecuteAsync(cmd);

        return Result.Success();
    }
}
