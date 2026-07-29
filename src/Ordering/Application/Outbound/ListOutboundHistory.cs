using Application.Abstracts;
using Davish.Result;
using Domain.OutboundOrders;

namespace Application.Outbound;

public sealed record ListOutboundHistoryQuery(
    Guid? WarehouseId,
    string? OrderNo,
    OutboundOrderStatus? Status,
    DateTime? RequestedFrom,
    DateTime? RequestedTo,
    DateTime? CompletedFrom,
    DateTime? CompletedTo,
    string? ProductNo,
    string? ProductName,
    string? Unit,
    string? RequestedByName,
    string? ConfirmedByName,
    int Page,
    int Size
) : IQuery<Result<PagedResult<OutboundHistoryDto>>>, ICacheableQuery
{
    public string CacheKey =>
        $"outbound-history:{HistoryCacheKey.WarehouseSegment(WarehouseId)}:{OrderNo}:{Status}:{RequestedFrom:O}:" +
        $"{RequestedTo:O}:{CompletedFrom:O}:{CompletedTo:O}:{ProductNo}:{ProductName}:{Unit}:{RequestedByName}:" +
        $"{ConfirmedByName}:{Page}:{Size}";

    // 歷程只會有已終結狀態(Confirmed/Rejected)的訂單,不會被回頭修改,只會有新的單子
    // 加進來——事件觸發的失效搭配這個短 TTL 當保險,不用擔心兩者互相矛盾。
    public TimeSpan CacheTtl => TimeSpan.FromSeconds(60);
}

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

public sealed class ListOutboundHistoryQueryHandler(
    IOutboundOrderReader reader,
    ICacher cacher
) : IQueryHandler<ListOutboundHistoryQuery, Result<PagedResult<OutboundHistoryDto>>>
{
    public async Task<Result<PagedResult<OutboundHistoryDto>>> HandleAsync(
        ListOutboundHistoryQuery request, CancellationToken cancellationToken)
    {
        var cached = await cacher.GetAsync<PagedResult<OutboundHistoryDto>>(request.CacheKey, cancellationToken);
        if (cached is not null)
            return Result.Success(cached);

        var result = await reader.ListHistoryAsync(
            request.WarehouseId,
            request.OrderNo,
            request.Status,
            request.RequestedFrom,
            request.RequestedTo,
            request.CompletedFrom,
            request.CompletedTo,
            request.ProductNo,
            request.ProductName,
            request.Unit,
            request.RequestedByName,
            request.ConfirmedByName,
            request.Page,
            request.Size,
            cancellationToken);

        if (result.IsSuccess)
            await cacher.SetAsync(request.CacheKey, result.Value, request.CacheTtl, cancellationToken);

        return result;
    }
}
