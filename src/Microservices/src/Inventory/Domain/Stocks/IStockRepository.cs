using Davish.Result;

namespace Domain.Stocks;

public interface IStockRepository
{
    Task<Result<Stock>> GetByProductAndWarehouseAsync(Guid productId, Guid warehouseId, CancellationToken ct);
    Task<Result> AddAsync(Stock stock, CancellationToken ct);
    Task<Result> SaveAsync(Stock stock, CancellationToken ct);

    Task<Result<IReadOnlyList<Stock>>> GetByProductsAndWarehouseAsync(
        IReadOnlyList<Guid> productIds, Guid warehouseId, CancellationToken ct);

    // 回傳值是「因為併發衝突而被跳過、沒有真的寫進去」的 ProductId 清單。
    // 空清單代表全部成功。
    Task<Result<IReadOnlyList<Guid>>> SaveRangeAsync(IReadOnlyList<Stock> stocks, CancellationToken ct);
}
