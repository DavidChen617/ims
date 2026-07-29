using Davish.Result;

namespace Application.Outbound;

// 支撐 Inventory 庫存畫面上的「待出庫」數字:某個商品/倉庫,目前所有 Pending 狀態出庫單的
// 品項數量加總。這個由 Ordering 自己負責 —— 只有它知道哪些出庫單還在等倉庫管理者確認。
public sealed record ListPendingOutboundQuantitiesQuery(
    Guid? WarehouseId,
    Guid? ProductId
) : IQuery<Result<PendingOutboundQuantitiesDto>>;

public sealed record PendingOutboundQuantityDto(Guid ProductId, Guid WarehouseId, int PendingQuantity);

public sealed record PendingOutboundQuantitiesDto(IReadOnlyList<PendingOutboundQuantityDto> Items);

public sealed class ListPendingOutboundQuantitiesQueryHandler(
    IOutboundOrderReader reader
) : IQueryHandler<ListPendingOutboundQuantitiesQuery, Result<PendingOutboundQuantitiesDto>>
{
    public async Task<Result<PendingOutboundQuantitiesDto>> HandleAsync(
        ListPendingOutboundQuantitiesQuery request, CancellationToken cancellationToken)
    {
        return await reader.ListPendingQuantitiesAsync(request.WarehouseId, request.ProductId, cancellationToken)
            .Then(items => new PendingOutboundQuantitiesDto(items));
    }
}
