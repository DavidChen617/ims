using Api.Extension;
using Application;
using Application.Outbound;
using Davish.Sendr;

namespace Api.Endpoints.v1.Outbound;

public static class ListWarehousePendingOutboundQuantitiesEndpoint
{
    extension(RouteGroupBuilder outboundV1Group)
    {
        public RouteGroupBuilder MapListWarehousePendingOutboundQuantitiesEndpoint()
        {
            outboundV1Group.MapGet("pending-quantities/warehouse", Handle)
                .Produces<PendingOutboundQuantitiesDto>()
                .WithName("ListWarehousePendingOutboundQuantities")
                .WithSummary("List pending-outbound quantities for the current warehouse")
                .WithDescription(
                    "For each product in the caller's warehouse, the summed quantity across all " +
                    "currently-Pending (awaiting warehouse admin confirm) outbound orders.")
                .RequireAuthorization("WarehouseStaffOnly");

            return outboundV1Group;
        }
    }

    private static async Task<IResult> Handle(
        ISender sender,
        ICurrentUser currentUser,
        CancellationToken ct,
        Guid? productId = null)
    {
        var result = await sender.SendAsync(
            new ListPendingOutboundQuantitiesQuery(currentUser.WarehouseId, productId), ct);

        return result.ToOk();
    }
}
