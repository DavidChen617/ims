using Application.Abstracts;
using Davish.Result;
using Domain.InboundOrders;

namespace Application.Inbound;

public sealed record ListInboundHistoryQuery(
    Guid? WarehouseId,
    string? OrderNo,
    string? ProductNo,
    string? ProductName,
    Guid? RequestedBy,
    Guid? ConfirmedBy,
    InboundOrderStatus? Status,
    DateTime? RequestedFrom,
    DateTime? RequestedTo,
    int? QuantityMin,
    int? QuantityMax,
    decimal? UnitPriceMin,
    decimal? UnitPriceMax,
    decimal? AmountMin,
    decimal? AmountMax,
    int Page,
    int Size
) : IQuery<Result<InboundHistoryResultDto>>, ICacheableQuery
{
    public string CacheKey =>
        $"inbound-history:{HistoryCacheKey.WarehouseSegment(WarehouseId)}:{OrderNo}:{ProductNo}:{ProductName}:" +
        $"{RequestedBy}:{ConfirmedBy}:{Status}:{RequestedFrom:O}:{RequestedTo:O}:{QuantityMin}:{QuantityMax}:" +
        $"{UnitPriceMin}:{UnitPriceMax}:{AmountMin}:{AmountMax}:{Page}:{Size}";

    public TimeSpan CacheTtl => TimeSpan.FromSeconds(60);
}

public sealed record InboundHistoryLineDto(
    string OrderNo,
    Guid ProductId,
    string ProductNo,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal TotalAmount);

public sealed record InboundHistoryResultDto(
    PagedResult<InboundHistoryLineDto> Page,
    int TotalQuantity,
    decimal TotalAmount);

public sealed class ListInboundHistoryQueryHandler(
    IInboundOrderReader reader,
    ICacher cacher
) : IQueryHandler<ListInboundHistoryQuery, Result<InboundHistoryResultDto>>
{
    public async Task<Result<InboundHistoryResultDto>> HandleAsync(
        ListInboundHistoryQuery request, CancellationToken cancellationToken)
    {
        var cached = await cacher.GetAsync<InboundHistoryResultDto>(request.CacheKey, cancellationToken);
        if (cached is not null)
            return Result.Success(cached);

        var result = await reader.ListHistoryAsync(
            request.WarehouseId,
            request.OrderNo,
            request.ProductNo,
            request.ProductName,
            request.RequestedBy,
            request.ConfirmedBy,
            request.Status,
            request.RequestedFrom,
            request.RequestedTo,
            request.QuantityMin,
            request.QuantityMax,
            request.UnitPriceMin,
            request.UnitPriceMax,
            request.AmountMin,
            request.AmountMax,
            request.Page,
            request.Size,
            cancellationToken);

        if (result.IsSuccess)
            await cacher.SetAsync(request.CacheKey, result.Value, request.CacheTtl, cancellationToken);

        return result;
    }
}
