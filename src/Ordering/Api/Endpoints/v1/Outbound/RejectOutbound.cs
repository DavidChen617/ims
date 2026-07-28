using Api.Extension;
using Application.Outbound;
using Davish.Sendr;

namespace Api.Endpoints.v1.Outbound;

public static class RejectOutboundEndpoint
{
    extension(RouteGroupBuilder outboundV1Group)
    {
        public RouteGroupBuilder MapRejectOutboundEndpoint()
        {
            outboundV1Group.MapPost("{id:guid}/reject", Handle)
                .Produces<RejectOutboundDto>()
                .Produces(StatusCodes.Status404NotFound)
                .Produces(StatusCodes.Status400BadRequest)
                .WithName("RejectOutbound")
                .WithSummary("Reject an outbound order")
                .WithDescription("Reject a pending outbound order with a reason.")
                .RequireAuthorization("WarehouseAdminOnly");

            return outboundV1Group;
        }
    }

    private static async Task<IResult> Handle(
        Guid id,
        RejectOutboundRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.SendAsync(new RejectOutboundCommand(id, request.Reason), ct);

        return result.ToOk();
    }
}

public record RejectOutboundRequest(string Reason);
