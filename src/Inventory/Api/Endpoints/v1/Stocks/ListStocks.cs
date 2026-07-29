using Api.Extension;
using Application;
using Application.Stocks;
using Davish.Sendr;

namespace Api.Endpoints.v1.Stocks;

public static class ListStocksEndpoint
{
    extension(RouteGroupBuilder stocksV1Group)
    {
        public RouteGroupBuilder MapListStocksEndpoint()
        {
            stocksV1Group.MapGet("", Handle)
                .Produces<PagedResult<StockDto>>()
                .WithName("ListStocks")
                .WithSummary("List stock levels across all warehouses")
                .WithDescription("List stock levels, optionally filtered by warehouse or product.")
                .RequireAuthorization("AdminOnly");

            return stocksV1Group;
        }
    }

    private static async Task<IResult> Handle(
        ISender sender,
        CancellationToken ct,
        [AsParameters] ListStocksRequest request)
    {
        var result = await sender.SendAsync(
            new ListStocksQuery(
                request.WarehouseId, request.ProductId, request.ProductNo, request.ProductName, request.Unit,
                request.QuantityMin, request.QuantityMax, request.CumulativeShippedMin, request.CumulativeShippedMax,
                request.Page ?? 1, request.Size ?? 20),
            ct);

        return result.ToOk();
    }
}

public sealed record ListStocksRequest(
    Guid? WarehouseId = null,
    Guid? ProductId = null,
    string? ProductNo = null,
    string? ProductName = null,
    string? Unit = null,
    int? QuantityMin = null,
    int? QuantityMax = null,
    int? CumulativeShippedMin = null,
    int? CumulativeShippedMax = null,
    int? Page = null,
    int? Size = null);
