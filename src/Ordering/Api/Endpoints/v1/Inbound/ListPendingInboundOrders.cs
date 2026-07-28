using Api.Extension;
using Application.Inbound;
using Davish.Sendr;

namespace Api.Endpoints.v1.Inbound;

public static class ListPendingInboundOrdersEndpoint
{
    extension(RouteGroupBuilder inboundV1Group)
    {
        public RouteGroupBuilder MapListPendingInboundOrdersEndpoint()
        {
            inboundV1Group.MapGet("pending", Handle)
                .Produces<PendingInboundOrdersDto>()
                .WithName("ListPendingInboundOrders")
                .WithSummary("List pending inbound orders")
                .WithDescription("List inbound orders awaiting review for the current warehouse.")
                .RequireAuthorization("WarehouseStaffOnly");

            return inboundV1Group;
        }
    }

    private static async Task<IResult> Handle(
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.SendAsync(new ListPendingInboundOrdersQuery(), ct);

        return result.ToOk();
    }
}
