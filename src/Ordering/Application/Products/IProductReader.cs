using Davish.Result;

namespace Application.Products;

public interface IProductReader
{
    Task<Result<ProductDto>> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Result<PagedResult<ProductDto>>> ListAsync(int page, int size, CancellationToken ct);
    Task<Result<IReadOnlyList<ProductUnitDto>>> ListUnitsAsync(CancellationToken ct);
}
