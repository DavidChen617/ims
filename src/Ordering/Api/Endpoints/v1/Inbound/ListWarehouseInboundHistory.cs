using Api.Extension;
using Application.Inbound;
using Davish.Sendr;
using Domain.InboundOrders;

using Application.Abstracts;
namespace Api.Endpoints.v1.Inbound;

public static class ListWarehouseInboundHistoryEndpoint
{
    extension(RouteGroupBuilder inboundV1Group)
    {
        public RouteGroupBuilder MapListWarehouseInboundHistoryEndpoint()
        {
            inboundV1Group.MapGet("history/warehouse", Handle)
                .Produces<InboundHistoryResultDto>()
                .WithName("ListWarehouseInboundHistory")
                .WithSummary("List inbound history for the current warehouse")
                .WithDescription(
                    "List completed (confirmed/rejected) inbound order lines, flattened by product, " +
                    "for the caller's warehouse.")
                .RequireAuthorization("WarehouseStaffOnly");

            return inboundV1Group;
        }
    }

    private static async Task<IResult> Handle(
        ISender sender,
        ICurrentUser currentUser,
        CancellationToken ct,
        [AsParameters] ListWarehouseInboundHistoryRequest request)
    {
        var result = await sender.SendAsync(
            new ListInboundHistoryQuery(
                currentUser.WarehouseId, request.OrderNo, request.ProductNo, request.ProductName, request.RequestedBy,
                request.ConfirmedBy, request.Status, request.RequestedFrom, request.RequestedTo,
                request.QuantityMin, request.QuantityMax, request.UnitPriceMin, request.UnitPriceMax,
                request.AmountMin, request.AmountMax, request.Page ?? 1, request.Size ?? 20),
            ct);

        return result.ToOk();
    }
}

public sealed record ListWarehouseInboundHistoryRequest(
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
