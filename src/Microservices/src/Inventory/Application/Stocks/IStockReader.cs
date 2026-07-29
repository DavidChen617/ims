using Davish.Result;

namespace Application.Stocks;

public interface IStockReader
{
    Task<Result<PagedResult<StockDto>>> ListAsync(
        Guid? warehouseId, Guid? productId, string? productNo, string? productName, string? unit,
        int? quantityMin, int? quantityMax, int? cumulativeShippedMin, int? cumulativeShippedMax,
        int page, int size, CancellationToken ct);
}
