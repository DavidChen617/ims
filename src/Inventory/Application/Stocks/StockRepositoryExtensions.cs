using Domain.Stocks;

namespace Application.Stocks;

internal static class StockRepositoryExtensions
{
    public static async Task<(Stock Stock, bool IsNew)> GetOrCreateAsync(
        this IStockRepository repository, Guid productId, Guid warehouseId, CancellationToken ct)
    {
        var result = await repository.GetByProductAndWarehouseAsync(productId, warehouseId, ct);

        return result.IsSuccess
            ? (result.Value, false)
            : (Stock.Create(productId, warehouseId).Value, true);
    }

    // 批次版本:一次查完整批 productId 缺哪些,缺的就地生成新的 Stock —— 不用再靠
    // AddAsync/SaveAsync 分開新增跟更新,呼叫端存的時候一律用 SaveRangeAsync 的
    // upsert 就好。
    public static async Task<IReadOnlyList<Stock>> GetOrCreateManyAsync(
        this IStockRepository repository, IReadOnlyList<Guid> productIds, Guid warehouseId, CancellationToken ct)
    {
        var existingResult = await repository.GetByProductsAndWarehouseAsync(productIds, warehouseId, ct);
        var existingByProductId = existingResult.Value.ToDictionary(s => s.ProductId);

        return productIds
            .Select(productId => existingByProductId.TryGetValue(productId, out var stock)
                ? stock
                : Stock.Create(productId, warehouseId).Value)
            .ToList();
    }
}
