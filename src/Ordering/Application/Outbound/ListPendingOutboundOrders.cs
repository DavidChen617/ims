using Davish.Result;

namespace Application.Outbound;

public sealed record ListPendingOutboundOrdersQuery : IQuery<Result<PendingOutboundOrdersDto>>;

public sealed record PendingOutboundOrderDto(Guid Id, string OrderNo);

public sealed record PendingOutboundOrdersDto(IReadOnlyList<PendingOutboundOrderDto> Items);

public sealed class ListPendingOutboundOrdersQueryHandler(
    IOutboundOrderReader reader,
    ICurrentUser currentUser
) : IQueryHandler<ListPendingOutboundOrdersQuery, Result<PendingOutboundOrdersDto>>
{
    public async Task<Result<PendingOutboundOrdersDto>> HandleAsync(
        ListPendingOutboundOrdersQuery request, CancellationToken cancellationToken)
    {
        return await reader.ListPendingAsync(currentUser.WarehouseId!.Value, cancellationToken)
            .Then(items => new PendingOutboundOrdersDto(items));
    }
}
