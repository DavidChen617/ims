using Api.Extension;
using Application.Inbound;
using Davish.Sendr;

namespace Api.Endpoints.v1.Inbound;

public static class GetInboundOrderEndpoint
{
    extension(RouteGroupBuilder inboundV1Group)
    {
        public RouteGroupBuilder MapGetInboundOrderEndpoint()
        {
            inboundV1Group.MapGet("{id:guid}", Handle)
                .Produces<InboundOrderDto>()
                .Produces(StatusCodes.Status404NotFound)
                .WithName("GetInboundOrder")
                .WithSummary("Get an inbound order")
                .WithDescription("Get an inbound order by id, scoped to the caller's own warehouse.")
                .RequireAuthorization("WarehouseStaffOnly");

            return inboundV1Group;
        }
    }

    private static async Task<IResult> Handle(
        Guid id,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.SendAsync(new GetInboundOrderQuery(id), ct);

        return result.ToOk();
    }
}
