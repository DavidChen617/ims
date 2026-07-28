using Api.Extension;
using Application.Outbound;
using Davish.Sendr;

namespace Api.Endpoints.v1.Outbound;

public static class ConfirmOutboundEndpoint
{
    extension(RouteGroupBuilder outboundV1Group)
    {
        public RouteGroupBuilder MapConfirmOutboundEndpoint()
        {
            outboundV1Group.MapPost("{id:guid}/confirm", Handle)
                .Produces<ConfirmOutboundDto>()
                .ProducesProblem(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .WithName("ConfirmOutbound")
                .WithSummary("Confirm an outbound order")
                .WithDescription("Confirm a pending outbound order.")
                .RequireAuthorization("WarehouseAdminOnly");

            return outboundV1Group;
        }
    }

    private static async Task<IResult> Handle(
        Guid id,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.SendAsync(new ConfirmOutboundCommand(id), ct);

        return result.ToOk();
    }
}
