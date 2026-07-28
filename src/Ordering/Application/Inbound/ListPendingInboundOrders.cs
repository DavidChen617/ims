using Davish.Result;

namespace Application.Inbound;

public sealed record ListPendingInboundOrdersQuery : IQuery<Result<PendingInboundOrdersDto>>;

public sealed record PendingInboundOrderDto(Guid Id, string OrderNo);

public sealed record PendingInboundOrdersDto(IReadOnlyList<PendingInboundOrderDto> Items);

public sealed class ListPendingInboundOrdersQueryHandler(
    IInboundOrderReader reader,
    ICurrentUser currentUser
) : IQueryHandler<ListPendingInboundOrdersQuery, Result<PendingInboundOrdersDto>>
{
    public async Task<Result<PendingInboundOrdersDto>> HandleAsync(
        ListPendingInboundOrdersQuery request, CancellationToken cancellationToken)
    {
        return await reader.ListPendingAsync(currentUser.WarehouseId!.Value, cancellationToken)
            .Then(items => new PendingInboundOrdersDto(items));
    }
}
