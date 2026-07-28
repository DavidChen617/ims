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
        Guid? warehouseId = null,
        Guid? productId = null,
        string? productNo = null,
        string? productName = null,
        int page = 1,
        int size = 20)
    {
        var result = await sender.SendAsync(
            new ListStocksQuery(warehouseId, productId, productNo, productName, page, size), ct);

        return result.ToOk();
    }
}
