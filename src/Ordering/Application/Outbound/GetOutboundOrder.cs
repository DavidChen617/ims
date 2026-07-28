using Davish.Result;

namespace Application.Outbound;

public sealed record GetOutboundOrderQuery(Guid Id) : IQuery<Result<OutboundOrderDto>>;

public sealed record OutboundOrderItemDto(
    Guid ProductId,
    string ProductNo,
    string ProductName,
    string Unit,
    int Quantity);

public sealed record OutboundOrderDto(
    Guid Id,
    string OrderNo,
    Guid WarehouseId,
    string Status,
    string? RejectReason,
    Guid RequestedBy,
    string RequestedByName,
    DateTime RequestedAt,
    Guid? ConfirmedBy,
    string? ConfirmedByName,
    DateTime? ConfirmedAt,
    IReadOnlyList<OutboundOrderItemDto> Items);

public sealed class GetOutboundOrderQueryHandler(
    IOutboundOrderReader reader,
    ICurrentUser currentUser
) : IQueryHandler<GetOutboundOrderQuery, Result<OutboundOrderDto>>
{
    public async Task<Result<OutboundOrderDto>> HandleAsync(GetOutboundOrderQuery request, CancellationToken cancellationToken)
    {
        return await reader.GetByIdAsync(request.Id, currentUser.WarehouseId!.Value, cancellationToken);
    }
}
