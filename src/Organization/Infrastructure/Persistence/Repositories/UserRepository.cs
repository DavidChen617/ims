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

    public async Task<Result<IReadOnlyList<User>>> ListPagedAsync(
        Guid? warehouseId, string? name, string? username, Role? role, string? warehouseName,
        DateTime? createdFrom, DateTime? createdTo, int page, int size, CancellationToken ct)
    {
        var cmd = new CommandDefinition(
            """
            select u.id, u.warehouse_id, u.name, u.username, u.password_hash, u.created_at, u.role
            from users u
            left join warehouse w on w.id = u.warehouse_id
            where (@WarehouseId is null or u.warehouse_id = @WarehouseId)
              and (@Name is null or u.name ilike '%' || @Name || '%')
              and (@Username is null or u.username ilike '%' || @Username || '%')
              and (@Role::smallint is null or u.role = @Role::smallint)
              and (@WarehouseName is null or w.name ilike '%' || @WarehouseName || '%')
              and (@CreatedFrom::timestamp is null or u.created_at >= @CreatedFrom::timestamp)
              and (@CreatedTo::timestamp is null or u.created_at <= @CreatedTo::timestamp)
            order by u.created_at
            limit @Size offset @Offset
            """,
            new
            {
                WarehouseId = warehouseId, Name = name, Username = username, Role = role, WarehouseName = warehouseName,
                CreatedFrom = createdFrom, CreatedTo = createdTo, Size = size, Offset = (page - 1) * size,
            },
            cancellationToken: ct,
            transaction: unitOfWork.Transaction
        );

        var users = await unitOfWork.Connection.QueryAsync<User>(cmd);

        return users.ToList().AsReadOnly();
    }

    public async Task<Result<int>> CountAsync(
        Guid? warehouseId, string? name, string? username, Role? role, string? warehouseName,
        DateTime? createdFrom, DateTime? createdTo, CancellationToken ct)
    {
        var cmd = new CommandDefinition(
            """
            select count(*)::int
            from users u
            left join warehouse w on w.id = u.warehouse_id
            where (@WarehouseId is null or u.warehouse_id = @WarehouseId)
              and (@Name is null or u.name ilike '%' || @Name || '%')
              and (@Username is null or u.username ilike '%' || @Username || '%')
              and (@Role::smallint is null or u.role = @Role::smallint)
              and (@WarehouseName is null or w.name ilike '%' || @WarehouseName || '%')
              and (@CreatedFrom::timestamp is null or u.created_at >= @CreatedFrom::timestamp)
              and (@CreatedTo::timestamp is null or u.created_at <= @CreatedTo::timestamp)
            """,
            new
            {
                WarehouseId = warehouseId, Name = name, Username = username, Role = role, WarehouseName = warehouseName,
                CreatedFrom = createdFrom, CreatedTo = createdTo,
            },
            cancellationToken: ct,
            transaction: unitOfWork.Transaction
        );

        return await unitOfWork.Connection.QuerySingleAsync<int>(cmd);
    }
}
