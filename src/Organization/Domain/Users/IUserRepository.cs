using Davish.Result;

namespace Domain.Users;

public interface IUserRepository
{
    Task<Result<User>> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Result<User>> GetByUsername(string username, CancellationToken ct);
    Task<Result> AddAsync(User user, CancellationToken ct);
    Task<Result<IReadOnlyList<User>>> ListAsync(Guid? warehouseId, CancellationToken ct);
}
