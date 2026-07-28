using System.Data;
using System.Data.Common;
using Npgsql;
using SharedKernel;

namespace Infrastructure.Persistence;

public interface IInventoryUnitOfWork : IUnitOfWork
{
    DbConnection Connection { get; }
    DbTransaction? Transaction { get; }
}

public class InventoryUnitOfWork(NpgsqlDataSource dataSource) : IInventoryUnitOfWork, IDisposable, IAsyncDisposable
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

        await Transaction.CommitAsync(ct);
        Transaction = null;
    }

    public async Task RollbackAsync(CancellationToken ct)
    {
        if (Transaction is null)
            return;

        await Transaction.RollbackAsync(ct);
        Transaction = null;
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
