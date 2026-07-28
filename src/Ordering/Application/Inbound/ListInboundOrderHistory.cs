using Davish.Result;
using Domain.InboundOrders;

namespace Application.Inbound;

// ListInboundHistoryQuery(依需求文件 v2.2 §7,按商品品項攤平)的訂單層級版本。
// 這支是給入庫作業畫面裡的「已處理清單」頁籤用的 —— 一張單一列,
// 對齊 ListOutboundHistoryQuery 本來就有的形狀。
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
) : IQuery<Result<InboundOrderHistoryResultDto>>;

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
    IInboundOrderReader reader
) : IQueryHandler<ListInboundOrderHistoryQuery, Result<InboundOrderHistoryResultDto>>
{
    public async Task<Result<InboundOrderHistoryResultDto>> HandleAsync(
        ListInboundOrderHistoryQuery request, CancellationToken cancellationToken)
    {
        return await reader.ListOrderHistoryAsync(
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
    }
}
