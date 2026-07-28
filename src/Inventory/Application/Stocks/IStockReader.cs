using Davish.Result;

namespace Application.Stocks;

public interface IStockReader
{
    Task<Result<PagedResult<StockDto>>> ListAsync(
        Guid? warehouseId, Guid? productId, string? productNo, string? productName,
        int page, int size, CancellationToken ct);
}
