using Davish.Result;

namespace Application.Stocks;

public sealed record ListStocksQuery(
    Guid? WarehouseId,
    Guid? ProductId,
    string? ProductNo,
    string? ProductName,
    int Page,
    int Size
) : IQuery<Result<PagedResult<StockDto>>>;

public sealed record StockDto(
    Guid ProductId,
    string? ProductNo,
    string? ProductName,
    string? Unit,
    Guid WarehouseId,
    string? WarehouseName,
    int Quantity,
    int CumulativeShipped);

public sealed class ListStocksQueryHandler(
    IStockReader reader
) : IQueryHandler<ListStocksQuery, Result<PagedResult<StockDto>>>
{
    public async Task<Result<PagedResult<StockDto>>> HandleAsync(
        ListStocksQuery request, CancellationToken cancellationToken)
    {
        return await reader.ListAsync(
            request.WarehouseId, request.ProductId, request.ProductNo, request.ProductName,
            request.Page, request.Size, cancellationToken);
    }
}
