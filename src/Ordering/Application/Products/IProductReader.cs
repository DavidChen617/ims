using Davish.Result;

namespace Application.Products;

public interface IProductReader
{
    Task<Result<ProductDto>> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Result<PagedResult<ProductDto>>> ListAsync(
        string? productNo, string? name, string? unit, decimal? priceMin, decimal? priceMax,
        int page, int size, CancellationToken ct);
    Task<Result<IReadOnlyList<ProductUnitDto>>> ListUnitsAsync(CancellationToken ct);
}
