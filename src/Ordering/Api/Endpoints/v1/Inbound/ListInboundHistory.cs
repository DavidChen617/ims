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
        Guid? warehouseId = null,
        string? productNo = null,
        string? productName = null,
        Guid? requestedBy = null,
        Guid? confirmedBy = null,
        InboundOrderStatus? status = null,
        DateTime? requestedFrom = null,
        DateTime? requestedTo = null,
        decimal? amountMin = null,
        decimal? amountMax = null,
        int page = 1,
        int size = 20)
    {
        var result = await sender.SendAsync(
            new ListInboundHistoryQuery(
                warehouseId, productNo, productName, requestedBy, confirmedBy, status,
                requestedFrom, requestedTo, amountMin, amountMax, page, size),
            ct);

        return result.ToOk();
    }
}
