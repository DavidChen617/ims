using Davish.Result;

namespace Domain.Warehouse;

public interface IWarehouseRepository
{
    Task<Result> AddAsync(Warehouse warehouse, CancellationToken ct);
    Task<Result<Warehouse>> GetByNameAsync(string name, CancellationToken ct);
    Task<Result<Warehouse>> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Result<IReadOnlyList<Warehouse>>> ListAsync(CancellationToken ct);

    Task<Result<IReadOnlyList<Warehouse>>> ListAsync(string? keyword, CancellationToken ct);
}
