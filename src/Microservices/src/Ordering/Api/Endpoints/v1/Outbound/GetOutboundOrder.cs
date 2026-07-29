using Api.Extension;
using Application.Outbound;
using Davish.Sendr;

namespace Api.Endpoints.v1.Outbound;

public static class GetOutboundOrderEndpoint
{
    extension(RouteGroupBuilder outboundV1Group)
    {
        public RouteGroupBuilder MapGetOutboundOrderEndpoint()
        {
            outboundV1Group.MapGet("{id:guid}", Handle)
                .Produces<OutboundOrderDto>()
                .ProducesProblem(StatusCodes.Status404NotFound)
                .WithName("GetOutboundOrder")
                .WithSummary("Get an outbound order")
                .WithDescription("Get an outbound order by id, scoped to the caller's own warehouse.")
                .RequireAuthorization("WarehouseStaffOnly");

            return outboundV1Group;
        }
    }

    private static async Task<IResult> Handle(
        Guid id,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.SendAsync(new GetOutboundOrderQuery(id), ct);

        return result.ToOk();
    }
}
