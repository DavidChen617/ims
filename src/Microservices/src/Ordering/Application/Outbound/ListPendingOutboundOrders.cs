using Davish.Result;

using Application.Abstracts;
namespace Application.Outbound;

public sealed record ListPendingOutboundOrdersQuery(int Page, int Size) : IQuery<Result<PagedResult<PendingOutboundOrderDto>>>;

public sealed record PendingOutboundOrderDto(Guid Id, string OrderNo);

public sealed class ListPendingOutboundOrdersQueryHandler(
    IOutboundOrderReader reader,
    ICurrentUser currentUser
) : IQueryHandler<ListPendingOutboundOrdersQuery, Result<PagedResult<PendingOutboundOrderDto>>>
{
    public async Task<Result<PagedResult<PendingOutboundOrderDto>>> HandleAsync(
        ListPendingOutboundOrdersQuery request, CancellationToken cancellationToken)
    {
        return await reader.ListPendingAsync(
            currentUser.WarehouseId!.Value, request.Page, request.Size, cancellationToken);
    }
}
