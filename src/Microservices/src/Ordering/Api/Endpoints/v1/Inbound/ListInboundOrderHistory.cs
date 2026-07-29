using Api.Extension;
using Application;
using Application.Inbound;
using Davish.Sendr;
using Domain.InboundOrders;

using Application.Abstracts;
namespace Api.Endpoints.v1.Inbound;

public static class ListInboundOrderHistoryEndpoint
{
    extension(RouteGroupBuilder inboundV1Group)
    {
        public RouteGroupBuilder MapListInboundOrderHistoryEndpoint()
        {
            inboundV1Group.MapGet("done", Handle)
                .Produces<InboundOrderHistoryResultDto>()
                .WithName("ListInboundOrderHistory")
                .WithSummary("List completed inbound orders for the current warehouse, one row per order")
                .WithDescription(
                    "Order-level counterpart to the flattened-by-product ListWarehouseInboundHistory — " +
                    "backs an 已處理清單-style view instead of the 入庫歷程 nav item.")
                .RequireAuthorization("WarehouseStaffOnly");

            return inboundV1Group;
        }
    }

    private static async Task<IResult> Handle(
        ISender sender,
        ICurrentUser currentUser,
        CancellationToken ct,
        [AsParameters] ListInboundOrderHistoryRequest request)
    {
        var result = await sender.SendAsync(
            new ListInboundOrderHistoryQuery(
                currentUser.WarehouseId!.Value, request.Status, request.ProductNo, request.ProductName,
                request.RequestedBy, request.ConfirmedBy, request.RequestedFrom, request.RequestedTo,
                request.CompletedFrom, request.CompletedTo, request.AmountMin, request.AmountMax),
            ct);

        return result.ToOk();
    }
}

public sealed record ListInboundOrderHistoryRequest(
    InboundOrderStatus? Status = null,
    string? ProductNo = null,
    string? ProductName = null,
    Guid? RequestedBy = null,
    Guid? ConfirmedBy = null,
    DateTime? RequestedFrom = null,
    DateTime? RequestedTo = null,
    DateTime? CompletedFrom = null,
    DateTime? CompletedTo = null,
    decimal? AmountMin = null,
    decimal? AmountMax = null);
