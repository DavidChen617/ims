using Api.Extension;
using Application;
using Application.Outbound;
using Davish.Sendr;
using Domain.OutboundOrders;

namespace Api.Endpoints.v1.Outbound;

public static class ListOutboundHistoryEndpoint
{
    extension(RouteGroupBuilder outboundV1Group)
    {
        public RouteGroupBuilder MapListOutboundHistoryEndpoint()
        {
            outboundV1Group.MapGet("history", Handle)
                .Produces<PagedResult<OutboundHistoryDto>>()
                .WithName("ListOutboundHistory")
                .WithSummary("List outbound history across all warehouses")
                .WithDescription("List completed (confirmed/rejected) outbound orders, optionally filtered by warehouse.")
                .RequireAuthorization("AdminOnly");

            return outboundV1Group;
        }
    }

    private static async Task<IResult> Handle(
        ISender sender,
        CancellationToken ct,
        [AsParameters] ListOutboundHistoryRequest request)
    {
        var result = await sender.SendAsync(
            new ListOutboundHistoryQuery(
                request.WarehouseId, request.OrderNo, request.Status, request.RequestedFrom, request.RequestedTo,
                request.CompletedFrom, request.CompletedTo, request.ProductNo, request.ProductName, request.Unit,
                request.RequestedByName, request.ConfirmedByName, request.Page ?? 1, request.Size ?? 20),
            ct);

        return result.ToOk();
    }
}

public sealed record ListOutboundHistoryRequest(
    Guid? WarehouseId = null,
    string? OrderNo = null,
    OutboundOrderStatus? Status = null,
    DateTime? RequestedFrom = null,
    DateTime? RequestedTo = null,
    DateTime? CompletedFrom = null,
    DateTime? CompletedTo = null,
    string? ProductNo = null,
    string? ProductName = null,
    string? Unit = null,
    string? RequestedByName = null,
    string? ConfirmedByName = null,
    int? Page = null,
    int? Size = null);
