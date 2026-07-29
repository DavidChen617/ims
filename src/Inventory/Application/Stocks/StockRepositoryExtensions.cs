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
