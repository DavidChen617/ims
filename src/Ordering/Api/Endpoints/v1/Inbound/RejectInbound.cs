using Api.Extension;
using Application.Inbound;
using Davish.Sendr;

namespace Api.Endpoints.v1.Inbound;

public static class RejectInboundEndpoint
{
    extension(RouteGroupBuilder inboundV1Group)
    {
        public RouteGroupBuilder MapRejectInboundEndpoint()
        {
            inboundV1Group.MapPost("{id:guid}/reject", Handle)
                .Produces<RejectInboundDto>()
                .Produces(StatusCodes.Status404NotFound)
                .Produces(StatusCodes.Status400BadRequest)
                .WithName("RejectInbound")
                .WithSummary("Reject an inbound order")
                .WithDescription("Reject a pending inbound order with a reason.")
                .RequireAuthorization("WarehouseAdminOnly");

            return inboundV1Group;
        }
    }

    private static async Task<IResult> Handle(
        Guid id,
        RejectInboundRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.SendAsync(new RejectInboundCommand(id, request.Reason), ct);

        return result.ToOk();
    }
}

public record RejectInboundRequest(string Reason);
