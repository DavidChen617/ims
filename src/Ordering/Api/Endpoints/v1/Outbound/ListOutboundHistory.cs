using Api.Extension;
using Application;
using Application.Outbound;
using Davish.Sendr;
using Domain.OutboundOrders;

namespace Api.Endpoints.v1.Outbound;

public static class ListOutboundHistoryEndpoint
{
    extension(RouteGroupBuilder outboundV1Group)
    {
        public RouteGroupBuilder MapListOutboundHistoryEndpoint()
        {
            outboundV1Group.MapGet("history", Handle)
                .Produces<PagedResult<OutboundHistoryDto>>()
                .WithName("ListOutboundHistory")
                .WithSummary("List outbound history across all warehouses")
                .WithDescription("List completed (confirmed/rejected) outbound orders, optionally filtered by warehouse.")
                .RequireAuthorization("AdminOnly");

            return outboundV1Group;
        }
    }

    private static async Task<IResult> Handle(
        ISender sender,
        CancellationToken ct,
        Guid? warehouseId = null,
        OutboundOrderStatus? status = null,
        DateTime? completedFrom = null,
        DateTime? completedTo = null,
        string? productNo = null,
        string? productName = null,
        string? unit = null,
        Guid? requestedBy = null,
        Guid? confirmedBy = null,
        int page = 1,
        int size = 20)
    {
        var result = await sender.SendAsync(
            new ListOutboundHistoryQuery(
                warehouseId, status, completedFrom, completedTo, productNo, productName, unit, requestedBy, confirmedBy,
                page, size),
            ct);

        return result.ToOk();
    }
}
