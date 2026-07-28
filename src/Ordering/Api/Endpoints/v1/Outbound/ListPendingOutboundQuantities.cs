using Api.Extension;
using Application.Outbound;
using Davish.Sendr;

namespace Api.Endpoints.v1.Outbound;

public static class ListPendingOutboundQuantitiesEndpoint
{
    extension(RouteGroupBuilder outboundV1Group)
    {
        public RouteGroupBuilder MapListPendingOutboundQuantitiesEndpoint()
        {
            outboundV1Group.MapGet("pending-quantities", Handle)
                .Produces<PendingOutboundQuantitiesDto>()
                .WithName("ListPendingOutboundQuantities")
                .WithSummary("List pending-outbound quantities across all warehouses")
                .WithDescription(
                    "For each product/warehouse, the summed quantity across all currently-Pending " +
                    "(awaiting warehouse admin confirm) outbound orders.")
                .RequireAuthorization("AdminOnly");

            return outboundV1Group;
        }
    }

    private static async Task<IResult> Handle(
        ISender sender,
        CancellationToken ct,
        Guid? warehouseId = null,
        Guid? productId = null)
    {
        var result = await sender.SendAsync(new ListPendingOutboundQuantitiesQuery(warehouseId, productId), ct);

        return result.ToOk();
    }
}
