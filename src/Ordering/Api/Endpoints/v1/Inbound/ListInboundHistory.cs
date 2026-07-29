using Api.Extension;
using Application.Inbound;
using Davish.Sendr;
using Domain.InboundOrders;

namespace Api.Endpoints.v1.Inbound;

public static class ListInboundHistoryEndpoint
{
    extension(RouteGroupBuilder inboundV1Group)
    {
        public RouteGroupBuilder MapListInboundHistoryEndpoint()
        {
            inboundV1Group.MapGet("history", Handle)
                .Produces<InboundHistoryResultDto>()
                .WithName("ListInboundHistory")
                .WithSummary("List inbound history across all warehouses")
                .WithDescription(
                    "List completed (confirmed/rejected) inbound order lines, flattened by product, " +
                    "optionally filtered by warehouse.")
                .RequireAuthorization("AdminOnly");

            return inboundV1Group;
        }
    }

    private static async Task<IResult> Handle(
        ISender sender,
        CancellationToken ct,
        [AsParameters] ListInboundHistoryRequest request)
    {
        var result = await sender.SendAsync(
            new ListInboundHistoryQuery(
                request.WarehouseId, request.OrderNo, request.ProductNo, request.ProductName, request.RequestedBy,
                request.ConfirmedBy, request.Status, request.RequestedFrom, request.RequestedTo,
                request.QuantityMin, request.QuantityMax, request.UnitPriceMin, request.UnitPriceMax,
                request.AmountMin, request.AmountMax, request.Page ?? 1, request.Size ?? 20),
            ct);

        return result.ToOk();
    }
}

public sealed record ListInboundHistoryRequest(
    Guid? WarehouseId = null,
    string? OrderNo = null,
    string? ProductNo = null,
    string? ProductName = null,
    Guid? RequestedBy = null,
    Guid? ConfirmedBy = null,
    InboundOrderStatus? Status = null,
    DateTime? RequestedFrom = null,
    DateTime? RequestedTo = null,
    int? QuantityMin = null,
    int? QuantityMax = null,
    decimal? UnitPriceMin = null,
    decimal? UnitPriceMax = null,
    decimal? AmountMin = null,
    decimal? AmountMax = null,
    int? Page = null,
    int? Size = null);
