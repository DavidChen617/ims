using Api.Extension;
using Application.Outbound;
using Davish.Sendr;

namespace Api.Endpoints.v1.Outbound;

public static class GetOutboundOrderForAdminEndpoint
{
    extension(RouteGroupBuilder outboundV1Group)
    {
        public RouteGroupBuilder MapGetOutboundOrderForAdminEndpoint()
        {
            outboundV1Group.MapGet("admin/{id:guid}", Handle)
                .Produces<OutboundOrderDto>()
                .ProducesProblem(StatusCodes.Status404NotFound)
                .WithName("GetOutboundOrderForAdmin")
                .WithSummary("Get an outbound order (admin)")
                .WithDescription("Get an outbound order by id, across any warehouse.")
                .RequireAuthorization("AdminOnly");

            return outboundV1Group;
        }
    }

    private static async Task<IResult> Handle(
        Guid id,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.SendAsync(new GetOutboundOrderForAdminQuery(id), ct);

        return result.ToOk();
    }
}
