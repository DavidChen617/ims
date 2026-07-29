using Api.Extension;
using Application;
using Application.Outbound;
using Davish.Sendr;
using Domain.OutboundOrders;

using Application.Abstracts;
namespace Api.Endpoints.v1.Outbound;

public static class ListWarehouseOutboundHistoryEndpoint
{
    extension(RouteGroupBuilder outboundV1Group)
    {
        public RouteGroupBuilder MapListWarehouseOutboundHistoryEndpoint()
        {
            outboundV1Group.MapGet("history/warehouse", Handle)
                .Produces<PagedResult<OutboundHistoryDto>>()
                .WithName("ListWarehouseOutboundHistory")
                .WithSummary("List outbound history for the current warehouse")
                .WithDescription("List completed (confirmed/rejected) outbound orders for the caller's warehouse.")
                .RequireAuthorization("WarehouseStaffOnly");

            return outboundV1Group;
        }
    }

    private static async Task<IResult> Handle(
        ISender sender,
        ICurrentUser currentUser,
        CancellationToken ct,
        [AsParameters] ListWarehouseOutboundHistoryRequest request)
    {
        var result = await sender.SendAsync(
            new ListOutboundHistoryQuery(
                currentUser.WarehouseId, request.OrderNo, request.Status, request.RequestedFrom, request.RequestedTo,
                request.CompletedFrom, request.CompletedTo, request.ProductNo, request.ProductName, request.Unit,
                request.RequestedByName, request.ConfirmedByName, request.Page ?? 1, request.Size ?? 20),
            ct);

        return result.ToOk();
    }
}

public sealed record ListWarehouseOutboundHistoryRequest(
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
