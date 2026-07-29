using Api.Extension;
using Application.Inbound;
using Davish.Sendr;

namespace Api.Endpoints.v1.Inbound;

public static class GetInboundOrderForAdminEndpoint
{
    extension(RouteGroupBuilder inboundV1Group)
    {
        public RouteGroupBuilder MapGetInboundOrderForAdminEndpoint()
        {
            inboundV1Group.MapGet("admin/{id:guid}", Handle)
                .Produces<InboundOrderDto>()
                .ProducesProblem(StatusCodes.Status404NotFound)
                .WithName("GetInboundOrderForAdmin")
                .WithSummary("Get an inbound order (admin)")
                .WithDescription("Get an inbound order by id, across any warehouse.")
                .RequireAuthorization("AdminOnly");

            return inboundV1Group;
        }
    }

    private static async Task<IResult> Handle(
        Guid id,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.SendAsync(new GetInboundOrderForAdminQuery(id), ct);

        return result.ToOk();
    }
}
