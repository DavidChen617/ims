using Davish.Result;

namespace Domain.Users;

public interface IUserRepository
{
    Task<Result<User>> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Result<User>> GetByUsername(string username, CancellationToken ct);
    Task<Result> AddAsync(User user, CancellationToken ct);
    Task<Result<IReadOnlyList<User>>> ListAsync(Guid? warehouseId, CancellationToken ct);

    Task<Result<IReadOnlyList<User>>> ListPagedAsync(
        Guid? warehouseId, string? name, string? username, Role? role, string? warehouseName,
        DateTime? createdFrom, DateTime? createdTo, int page, int size, CancellationToken ct);

    Task<Result<int>> CountAsync(
        Guid? warehouseId, string? name, string? username, Role? role, string? warehouseName,
        DateTime? createdFrom, DateTime? createdTo, CancellationToken ct);
}
