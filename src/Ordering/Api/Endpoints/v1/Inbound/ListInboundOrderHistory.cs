using Api.Extension;
using Application;
using Application.Inbound;
using Davish.Sendr;
using Domain.InboundOrders;

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
        InboundOrderStatus? status = null,
        string? productNo = null,
        string? productName = null,
        Guid? requestedBy = null,
        Guid? confirmedBy = null,
        DateTime? requestedFrom = null,
        DateTime? requestedTo = null,
        DateTime? completedFrom = null,
        DateTime? completedTo = null,
        decimal? amountMin = null,
        decimal? amountMax = null)
    {
        var result = await sender.SendAsync(
            new ListInboundOrderHistoryQuery(
                currentUser.WarehouseId!.Value, status, productNo, productName, requestedBy, confirmedBy,
                requestedFrom, requestedTo, completedFrom, completedTo, amountMin, amountMax),
            ct);

        return result.ToOk();
    }
}
