using Api.Extension;
using Application;
using Application.Outbound;
using Davish.Sendr;
using Domain.OutboundOrders;

using Application.Abstracts;
namespace Api.Endpoints.v1.Outbound;

public static class ListWarehouseOutboundHistoryEndpoint
{
    extension(RouteGroupBuilder outboundV1Group)
    {
        public RouteGroupBuilder MapListWarehouseOutboundHistoryEndpoint()
        {
            outboundV1Group.MapGet("history/warehouse", Handle)
                .Produces<PagedResult<OutboundHistoryDto>>()
                .WithName("ListWarehouseOutboundHistory")
                .WithSummary("List outbound history for the current warehouse")
                .WithDescription("List completed (confirmed/rejected) outbound orders for the caller's warehouse.")
                .RequireAuthorization("WarehouseStaffOnly");

            return outboundV1Group;
        }
    }

    private static async Task<IResult> Handle(
        ISender sender,
        ICurrentUser currentUser,
        CancellationToken ct,
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
                currentUser.WarehouseId, status, completedFrom, completedTo, productNo, productName, unit, requestedBy,
                confirmedBy, page, size),
            ct);

        return result.ToOk();
    }
}
