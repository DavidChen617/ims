using Davish.Result;

namespace Domain.Stocks;

public interface IStockRepository
{
    Task<Result<Stock>> GetByProductAndWarehouseAsync(Guid productId, Guid warehouseId, CancellationToken ct);
    Task<Result> AddAsync(Stock stock, CancellationToken ct);
    Task<Result> SaveAsync(Stock stock, CancellationToken ct);

    // 批次版本 —— 一個事件可能帶多個品項,逐筆查詢/寫入會是 N+1,這兩個方法把整批
    // 品項用一次 round-trip 處理完。
    Task<Result<IReadOnlyList<Stock>>> GetByProductsAndWarehouseAsync(
        IReadOnlyList<Guid> productIds, Guid warehouseId, CancellationToken ct);
    Task<Result> SaveRangeAsync(IReadOnlyList<Stock> stocks, CancellationToken ct);
}
