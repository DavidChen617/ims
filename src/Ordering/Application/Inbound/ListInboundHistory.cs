using Application.Abstracts;
using Davish.Result;
using Domain.InboundOrders;

namespace Application.Inbound;

public sealed record ListInboundHistoryQuery(
    Guid? WarehouseId,
    string? ProductNo,
    string? ProductName,
    Guid? RequestedBy,
    Guid? ConfirmedBy,
    InboundOrderStatus? Status,
    DateTime? RequestedFrom,
    DateTime? RequestedTo,
    decimal? AmountMin,
    decimal? AmountMax,
    int Page,
    int Size
) : IQuery<Result<InboundHistoryResultDto>>, ICacheableQuery
{
    public string CacheKey =>
        $"inbound-history:{HistoryCacheKey.WarehouseSegment(WarehouseId)}:{ProductNo}:{ProductName}:{RequestedBy}:" +
        $"{ConfirmedBy}:{Status}:{RequestedFrom:O}:{RequestedTo:O}:{AmountMin}:{AmountMax}:{Page}:{Size}";

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
    IInboundOrderReader reader
) : IQueryHandler<ListInboundHistoryQuery, Result<InboundHistoryResultDto>>
{
    public async Task<Result<InboundHistoryResultDto>> HandleAsync(
        ListInboundHistoryQuery request, CancellationToken cancellationToken)
    {
        return await reader.ListHistoryAsync(
            request.WarehouseId,
            request.ProductNo,
            request.ProductName,
            request.RequestedBy,
            request.ConfirmedBy,
            request.Status,
            request.RequestedFrom,
            request.RequestedTo,
            request.AmountMin,
            request.AmountMax,
            request.Page,
            request.Size,
            cancellationToken);
    }
}
