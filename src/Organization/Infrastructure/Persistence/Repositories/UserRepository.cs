using Dapper;
using Davish.Result;
using Domain.Users;
using Infrastructure.Persistence;

namespace Infrastructure.Persistence.Repositories;

public sealed class UserRepository(IOrganizationUnitOfWork unitOfWork) : IUserRepository
{
    public async Task<Result<User>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var cmd = new CommandDefinition(
            """
            select id, warehouse_id, name, username, password_hash, created_at, role
            from users
            where id = @Id
            """,
            new { Id = id },
            cancellationToken: ct,
            transaction: unitOfWork.Transaction
        );

        var user = await unitOfWork.Connection.QuerySingleOrDefaultAsync<User>(cmd);

        return user;
    }

    public async Task<Result<User>> GetByUsername(string username, CancellationToken ct)
    {
        var cmd = new CommandDefinition(
            """
            select id, warehouse_id, name, username, password_hash, created_at, role
            from users
            where username = @Username
            """,
            new { Username = username },
            cancellationToken: ct,
            transaction: unitOfWork.Transaction
        );

        var user = await unitOfWork.Connection.QuerySingleOrDefaultAsync<User>(cmd);

        return user;
    }

    public async Task<Result> AddAsync(User user, CancellationToken ct)
    {
        var cmd = new CommandDefinition(
            """
            insert into users(id, warehouse_id, name, username, password_hash, created_at, role)
            values(@Id, @WarehouseId, @Name, @Username, @PasswordHash, @CreatedAt, @Role)
            """,
            user,
            cancellationToken: ct,
            transaction: unitOfWork.Transaction
        );

        await unitOfWork.Connection.ExecuteAsync(cmd);

        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<User>>> ListAsync(Guid? warehouseId, CancellationToken ct)
    {
        var cmd = new CommandDefinition(
            """
            select id, warehouse_id, name, username, password_hash, created_at, role
            from users
            where @WarehouseId is null or warehouse_id = @WarehouseId
            """,
            new { WarehouseId = warehouseId },
            cancellationToken: ct,
            transaction: unitOfWork.Transaction
        );

        var users = await unitOfWork.Connection.QueryAsync<User>(cmd);

        return users.ToList().AsReadOnly();
    }
}
