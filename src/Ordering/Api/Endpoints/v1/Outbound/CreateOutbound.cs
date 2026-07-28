using Api.Extension;
using Application.Outbound;
using Davish.Sendr;

namespace Api.Endpoints.v1.Outbound;

public static class CreateOutboundEndpoint
{
    extension(RouteGroupBuilder outboundV1Group)
    {
        public RouteGroupBuilder MapCreateOutboundEndpoint()
        {
            outboundV1Group.MapPost("", Handle)
                .Produces<CreateOutboundDto>()
                .Produces(StatusCodes.Status400BadRequest)
                .WithName("CreateOutbound")
                .WithSummary("Create an outbound order")
                .WithDescription("Create a new outbound order for a warehouse with a list of product items.")
                .RequireAuthorization("WarehouseUserOnly");

            return outboundV1Group;
        }
    }

    private static async Task<IResult> Handle(
        CreateOutboundCommand command,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.SendAsync(command, ct);

        return result.ToCreatedAtRoute("GetOutboundOrder", x => new { id = x.Id });
    }
}
