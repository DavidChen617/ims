using Davish.Result;
using Domain.OutboundOrders;

namespace Application.Outbound;

public sealed record ListOutboundHistoryQuery(
    Guid? WarehouseId,
    OutboundOrderStatus? Status,
    DateTime? CompletedFrom,
    DateTime? CompletedTo,
    string? ProductNo,
    string? ProductName,
    string? Unit,
    Guid? RequestedBy,
    Guid? ConfirmedBy
) : IQuery<Result<OutboundHistoryResultDto>>;

public sealed record OutboundHistoryDto(
    Guid Id,
    string OrderNo,
    Guid WarehouseId,
    string Status,
    DateTime RequestedAt,
    DateTime? ConfirmedAt,
    Guid RequestedBy,
    string RequestedByName,
    Guid? ConfirmedBy,
    string? ConfirmedByName);

public sealed record OutboundHistoryResultDto(IReadOnlyList<OutboundHistoryDto> Items);

public sealed class ListOutboundHistoryQueryHandler(
    IOutboundOrderReader reader
) : IQueryHandler<ListOutboundHistoryQuery, Result<OutboundHistoryResultDto>>
{
    public async Task<Result<OutboundHistoryResultDto>> HandleAsync(
        ListOutboundHistoryQuery request, CancellationToken cancellationToken)
    {
        return await reader.ListHistoryAsync(
            request.WarehouseId,
            request.Status,
            request.CompletedFrom,
            request.CompletedTo,
            request.ProductNo,
            request.ProductName,
            request.Unit,
            request.RequestedBy,
            request.ConfirmedBy,
            cancellationToken)
            .Then(items => new OutboundHistoryResultDto(items));
    }
}
