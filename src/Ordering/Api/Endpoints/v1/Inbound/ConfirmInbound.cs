using Api.Extension;
using Application.Inbound;
using Davish.Sendr;

namespace Api.Endpoints.v1.Inbound;

public static class ConfirmInboundEndpoint
{
    extension(RouteGroupBuilder inboundV1Group)
    {
        public RouteGroupBuilder MapConfirmInboundEndpoint()
        {
            inboundV1Group.MapPost("{id:guid}/confirm", Handle)
                .Produces<ConfirmInboundDto>()
                .ProducesProblem(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .WithName("ConfirmInbound")
                .WithSummary("Confirm an inbound order")
                .WithDescription("Confirm a pending inbound order.")
                .RequireAuthorization("WarehouseAdminOnly");

            return inboundV1Group;
        }
    }

    private static async Task<IResult> Handle(
        Guid id,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.SendAsync(new ConfirmInboundCommand(id), ct);

        return result.ToOk();
    }
}
