using Api.Extension;
using Application;
using Application.Outbound;
using Davish.Sendr;

namespace Api.Endpoints.v1.Outbound;

public static class ListPendingOutboundOrdersEndpoint
{
    extension(RouteGroupBuilder outboundV1Group)
    {
        public RouteGroupBuilder MapListPendingOutboundOrdersEndpoint()
        {
            outboundV1Group.MapGet("pending", Handle)
                .Produces<PagedResult<PendingOutboundOrderDto>>()
                .WithName("ListPendingOutboundOrders")
                .WithSummary("List pending outbound orders")
                .WithDescription("List outbound orders awaiting review for the current warehouse.")
                .RequireAuthorization("WarehouseStaffOnly");

            return outboundV1Group;
        }
    }

    private static async Task<IResult> Handle(
        ISender sender,
        CancellationToken ct,
        int page = 1,
        int size = 20)
    {
        var result = await sender.SendAsync(new ListPendingOutboundOrdersQuery(page, size), ct);

        return result.ToOk();
    }
}
