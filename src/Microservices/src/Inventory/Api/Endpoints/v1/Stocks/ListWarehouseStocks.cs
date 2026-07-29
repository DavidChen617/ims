using Api.Extension;
using Application;
using Application.Stocks;
using Davish.Sendr;

using Application.Abstracts;
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
        [AsParameters] ListWarehouseStocksRequest request)
    {
        var result = await sender.SendAsync(
            new ListStocksQuery(
                currentUser.WarehouseId, request.ProductId, request.ProductNo, request.ProductName, request.Unit,
                request.QuantityMin, request.QuantityMax, request.CumulativeShippedMin, request.CumulativeShippedMax,
                request.Page ?? 1, request.Size ?? 20),
            ct);

        return result.ToOk();
    }
}

public sealed record ListWarehouseStocksRequest(
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
