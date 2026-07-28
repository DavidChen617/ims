using Davish.Result;

namespace Application.Inbound;

public sealed record GetInboundOrderQuery(Guid Id) : IQuery<Result<InboundOrderDto>>;

public sealed record InboundOrderItemDto(
    Guid ProductId,
    string ProductNo,
    string ProductName,
    string Unit,
    int Quantity,
    decimal UnitPrice,
    decimal TotalAmount);

public sealed record InboundOrderDto(
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
    IReadOnlyList<InboundOrderItemDto> Items);

public sealed class GetInboundOrderQueryHandler(
    IInboundOrderReader reader,
    ICurrentUser currentUser
) : IQueryHandler<GetInboundOrderQuery, Result<InboundOrderDto>>
{
    public async Task<Result<InboundOrderDto>> HandleAsync(GetInboundOrderQuery request, CancellationToken cancellationToken)
    {
        return await reader.GetByIdAsync(request.Id, currentUser.WarehouseId!.Value, cancellationToken);
    }
}
