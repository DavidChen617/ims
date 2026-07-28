using Api.Extension;
using Application;
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
                currentUser.WarehouseId, productNo, productName, requestedBy, confirmedBy, status,
                requestedFrom, requestedTo, amountMin, amountMax, page, size),
            ct);

        return result.ToOk();
    }
}
