using Davish.Result;

namespace Domain.Stocks;

public interface IStockRepository
{
    Task<Result<Stock>> GetByProductAndWarehouseAsync(Guid productId, Guid warehouseId, CancellationToken ct);
    Task<Result> AddAsync(Stock stock, CancellationToken ct);
    Task<Result> SaveAsync(Stock stock, CancellationToken ct);

    Task<Result<IReadOnlyList<Stock>>> GetByProductsAndWarehouseAsync(
        IReadOnlyList<Guid> productIds, Guid warehouseId, CancellationToken ct);
    Task<Result> SaveRangeAsync(IReadOnlyList<Stock> stocks, CancellationToken ct);
}
