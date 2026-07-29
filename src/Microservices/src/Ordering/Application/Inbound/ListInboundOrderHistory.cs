using Application.Abstracts;
using Davish.Result;
using Domain.InboundOrders;

namespace Application.Inbound;

public sealed record ListInboundOrderHistoryQuery(
    Guid WarehouseId,
    InboundOrderStatus? Status,
    string? ProductNo,
    string? ProductName,
    Guid? RequestedBy,
    Guid? ConfirmedBy,
    DateTime? RequestedFrom,
    DateTime? RequestedTo,
    DateTime? CompletedFrom,
    DateTime? CompletedTo,
    decimal? AmountMin,
    decimal? AmountMax
) : IQuery<Result<InboundOrderHistoryResultDto>>, ICacheableQuery
{
    public string CacheKey =>
        $"inbound-order-history:{WarehouseId}:{Status}:{ProductNo}:{ProductName}:{RequestedBy}:{ConfirmedBy}:" +
        $"{RequestedFrom:O}:{RequestedTo:O}:{CompletedFrom:O}:{CompletedTo:O}:{AmountMin}:{AmountMax}";

    public TimeSpan CacheTtl => TimeSpan.FromSeconds(60);
}

public sealed record InboundOrderHistoryDto(
    Guid Id,
    string OrderNo,
    Guid WarehouseId,
    string Status,
    string? RejectReason,
    DateTime RequestedAt,
    Guid RequestedBy,
    string RequestedByName,
    DateTime? ConfirmedAt,
    Guid? ConfirmedBy,
    string? ConfirmedByName,
    decimal TotalAmount);

public sealed record InboundOrderHistoryResultDto(IReadOnlyList<InboundOrderHistoryDto> Items);

public sealed class ListInboundOrderHistoryQueryHandler(
    IInboundOrderReader reader,
    ICacher cacher
) : IQueryHandler<ListInboundOrderHistoryQuery, Result<InboundOrderHistoryResultDto>>
{
    public async Task<Result<InboundOrderHistoryResultDto>> HandleAsync(
        ListInboundOrderHistoryQuery request, CancellationToken cancellationToken)
    {
        var cached = await cacher.GetAsync<InboundOrderHistoryResultDto>(request.CacheKey, cancellationToken);
        if (cached is not null)
            return Result.Success(cached);

        var result = await reader.ListOrderHistoryAsync(
            request.WarehouseId,
            request.Status,
            request.ProductNo,
            request.ProductName,
            request.RequestedBy,
            request.ConfirmedBy,
            request.RequestedFrom,
            request.RequestedTo,
            request.CompletedFrom,
            request.CompletedTo,
            request.AmountMin,
            request.AmountMax,
            cancellationToken)
            .Then(items => new InboundOrderHistoryResultDto(items));

        if (result.IsSuccess)
            await cacher.SetAsync(request.CacheKey, result.Value, request.CacheTtl, cancellationToken);

        return result;
    }
}
