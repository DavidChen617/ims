using Davish.Result;

using Application.Abstracts;
namespace Application.Inbound;

public sealed record ListPendingInboundOrdersQuery(int Page, int Size) : IQuery<Result<PagedResult<PendingInboundOrderDto>>>;

public sealed record PendingInboundOrderDto(Guid Id, string OrderNo);

public sealed class ListPendingInboundOrdersQueryHandler(
    IInboundOrderReader reader,
    ICurrentUser currentUser
) : IQueryHandler<ListPendingInboundOrdersQuery, Result<PagedResult<PendingInboundOrderDto>>>
{
    public async Task<Result<PagedResult<PendingInboundOrderDto>>> HandleAsync(
        ListPendingInboundOrdersQuery request, CancellationToken cancellationToken)
    {
        return await reader.ListPendingAsync(
            currentUser.WarehouseId!.Value, request.Page, request.Size, cancellationToken);
    }
}
