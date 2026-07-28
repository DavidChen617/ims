using System.Data;
using System.Data.Common;
using Davish.Sendr;
using Npgsql;
using SharedKernel;

namespace Infrastructure.Persistence;

public interface IOrderingUnitOfWork : IUnitOfWork
{
    DbConnection Connection { get; }
    DbTransaction? Transaction { get; }
}

public class OrderingUnitOfWork(
    NpgsqlDataSource dataSource,
    IAggregateRootChangeTracker tracker,
    IPublisher publisher
) : IOrderingUnitOfWork, IDisposable, IAsyncDisposable
{
    public DbConnection Connection { get; } = dataSource.CreateConnection();
    public DbTransaction? Transaction { get; private set; }

    public async Task BeginAsync(CancellationToken ct)
    {
        if (Transaction is not null)
            throw new InvalidOperationException("The transaction is already committed");

        if (Connection.State != ConnectionState.Open)
            await Connection.OpenAsync(ct);

        Transaction = await Connection.BeginTransactionAsync(ct);
    }

    public async Task CommitAsync(CancellationToken ct)
    {
        if (Transaction is null)
            throw new InvalidOperationException("There is no transaction to commit");

        var events = tracker
            .Dequeue()
            .SelectMany(x => x.DomainEvents)
            .ToList();

        if (events.Count > 0)
            foreach (var @event in events)
                await publisher.PublishAsync(@event, ct);

        await Transaction.CommitAsync(ct);
    }

    public async Task RollbackAsync(CancellationToken ct)
    {
        if (Transaction is null)
            return;

        await Transaction.RollbackAsync(ct);
    }

    public void Dispose()
    {
        Transaction?.Dispose();
        Connection.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (Transaction != null)
        {
            await Transaction.DisposeAsync();
        }

        await Connection.DisposeAsync();
    }
}
