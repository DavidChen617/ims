using Api.Extension;
using Application.Inbound;
using Davish.Sendr;

namespace Api.Endpoints.v1.Inbound;

public static class CreateInboundEndpoint
{
    extension(RouteGroupBuilder inboundV1Group)
    {
        public RouteGroupBuilder MapCreateInboundEndpoint()
        {
            inboundV1Group.MapPost("", Handle)
                .Produces<CreateInboundDto>()
                .Produces(StatusCodes.Status400BadRequest)
                .WithName("CreateInbound")
                .WithSummary("Create an inbound order")
                .WithDescription("Create a new inbound order for a warehouse with a list of product items.")
                .RequireAuthorization("WarehouseUserOnly");

            return inboundV1Group;
        }
    }

    private static async Task<IResult> Handle(
        CreateInboundCommand command,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.SendAsync(command, ct);

        return result.ToCreatedAtRoute("GetInboundOrder", x => new { id = x.Id });
    }
}
