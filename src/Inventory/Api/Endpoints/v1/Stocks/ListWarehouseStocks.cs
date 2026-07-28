using Api.Extension;
using Application;
using Application.Stocks;
using Davish.Sendr;

namespace Api.Endpoints.v1.Stocks;

public static class ListWarehouseStocksEndpoint
{
    extension(RouteGroupBuilder stocksV1Group)
    {
        public RouteGroupBuilder MapListWarehouseStocksEndpoint()
        {
            stocksV1Group.MapGet("warehouse", Handle)
                .Produces<PagedResult<StockDto>>()
                .WithName("ListWarehouseStocks")
                .WithSummary("List stock levels for the current warehouse")
                .WithDescription("List stock levels for the caller's warehouse, optionally filtered by product.")
                .RequireAuthorization("WarehouseStaffOnly");

            return stocksV1Group;
        }
    }

    private static async Task<IResult> Handle(
        ISender sender,
        ICurrentUser currentUser,
        CancellationToken ct,
        Guid? productId = null,
        string? productNo = null,
        string? productName = null,
        int page = 1,
        int size = 20)
    {
        var result = await sender.SendAsync(
            new ListStocksQuery(currentUser.WarehouseId, productId, productNo, productName, page, size), ct);

        return result.ToOk();
    }
}
