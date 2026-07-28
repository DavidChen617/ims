using Dapper;
using Davish.Result;
using Domain.Warehouse;
using Infrastructure.Persistence;

namespace Infrastructure.Persistence.Repositories;

public sealed class WarehouseRepository(IOrganizationUnitOfWork unitOfWork) : IWarehouseRepository
{
    public async Task<Result> AddAsync(Warehouse warehouse, CancellationToken ct)
    {
        var cmd = new CommandDefinition(
            """
            insert into warehouse(id, name)
            values(@Id, @Name)
            """,
            warehouse,
            cancellationToken: ct,
            transaction: unitOfWork.Transaction
        );

        await unitOfWork.Connection.ExecuteAsync(cmd);

        return Result.Success();
    }

    public async Task<Result<Warehouse>> GetByNameAsync(string name, CancellationToken ct)
    {
        var cmd = new CommandDefinition(
            """
            select id, name
            from warehouse
            where name = @Name
            """,
            new { Name = name },
            cancellationToken: ct,
            transaction: unitOfWork.Transaction
        );

        var warehouse = await unitOfWork.Connection.QuerySingleOrDefaultAsync<Warehouse>(cmd);

        return warehouse;
    }

    public async Task<Result<Warehouse>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var cmd = new CommandDefinition(
            """
            select id, name
            from warehouse
            where id = @Id
            """,
            new { Id = id },
            cancellationToken: ct,
            transaction: unitOfWork.Transaction
        );

        var warehouse = await unitOfWork.Connection.QuerySingleOrDefaultAsync<Warehouse>(cmd);

        return warehouse;
    }

    public async Task<Result<IReadOnlyList<Warehouse>>> ListAsync(CancellationToken ct)
    {
        var cmd = new CommandDefinition(
            """
            select id, name
            from warehouse
            """,
            cancellationToken: ct,
            transaction: unitOfWork.Transaction
        );

        var warehouses = await unitOfWork.Connection.QueryAsync<Warehouse>(cmd);

        return Result.Success<IReadOnlyList<Warehouse>>(warehouses.ToList());
    }
}
