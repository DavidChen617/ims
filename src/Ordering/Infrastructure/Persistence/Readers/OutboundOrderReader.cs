using Application;
using Application.Outbound;
using Dapper;
using Davish.Result;
using Domain.OutboundOrders;

namespace Infrastructure.Persistence.Readers;

public sealed class OutboundOrderReader(IOrderingUnitOfWork unitOfWork) : IOutboundOrderReader
{
    public Task<Result<OutboundOrderDto>> GetByIdAsync(Guid id, CancellationToken ct)
        => GetByIdCoreAsync(id, null, ct);

    public Task<Result<OutboundOrderDto>> GetByIdAsync(Guid id, Guid warehouseId, CancellationToken ct)
        => GetByIdCoreAsync(id, warehouseId, ct);

    private async Task<Result<OutboundOrderDto>> GetByIdCoreAsync(Guid id, Guid? warehouseId, CancellationToken ct)
    {
        var cmd = new CommandDefinition(
            """
            select o.id, o.order_no, o.warehouse_id, o.status, o.reject_reason,
                   o.requested_by, o.requested_by_name, o.requested_at,
                   o.confirmed_by, o.confirmed_by_name, o.confirmed_at,
                   i.product_id, i.quantity,
                   p.product_no, p.name, p.unit
            from outbound_orders o
            left join outbound_order_items i on i.outbound_order_id = o.id
            left join products p on p.id = i.product_id
            where o.id = @Id and (@WarehouseId is null or o.warehouse_id = @WarehouseId)
            """,
            new { Id = id, WarehouseId = warehouseId },
            cancellationToken: ct,
            transaction: unitOfWork.Transaction
        );

        OrderRow? header = null;
        var items = new List<OutboundOrderItemDto>();

        await unitOfWork.Connection.QueryAsync<OrderRow, ItemRow, ProductRow, OrderRow>(
            cmd,
            (row, item, product) =>
            {
                header ??= row;

                if (item is not null)
                    items.Add(new OutboundOrderItemDto(
                        item.ProductId,
                        product?.ProductNo ?? string.Empty,
                        product?.Name ?? string.Empty,
                        product?.Unit ?? string.Empty,
                        item.Quantity));

                return row;
            },
            splitOn: "product_id,product_no"
        );

        if (header is null)
            return new Error("OutboundOrder.NotFound", "Outbound order not found", ErrorType.NotFound);

        return new OutboundOrderDto(
            header.Id,
            header.OrderNo,
            header.WarehouseId,
            ((OutboundOrderStatus)header.Status).ToString(),
            header.RejectReason,
            header.RequestedBy,
            header.RequestedByName,
            header.RequestedAt,
            header.ConfirmedBy,
            header.ConfirmedByName,
            header.ConfirmedAt,
            items);
    }

    public async Task<Result<PagedResult<PendingOutboundOrderDto>>> ListPendingAsync(
        Guid warehouseId, int page, int size, CancellationToken ct)
    {
        var cmd = new CommandDefinition(
            """
            select id, order_no, count(*) over()::int as total_count
            from outbound_orders
            where warehouse_id = @WarehouseId and status = @Status
            order by requested_at
            limit @Size offset @Offset
            """,
            new { WarehouseId = warehouseId, Status = OutboundOrderStatus.Pending, Size = size, Offset = (page - 1) * size },
            cancellationToken: ct,
            transaction: unitOfWork.Transaction
        );

        var rows = (await unitOfWork.Connection.QueryAsync<PendingRow>(cmd)).ToList();
        var items = rows.Select(r => new PendingOutboundOrderDto(r.Id, r.OrderNo)).ToList();
        var totalCount = rows.Count > 0 ? rows[0].TotalCount : 0;

        return new PagedResult<PendingOutboundOrderDto>(items, totalCount, page, size);
    }

    private sealed record PendingRow(Guid Id, string OrderNo, int TotalCount);

    public async Task<Result<PagedResult<OutboundHistoryDto>>> ListHistoryAsync(
        Guid? warehouseId,
        string? orderNo,
        OutboundOrderStatus? status,
        DateTime? requestedFrom,
        DateTime? requestedTo,
        DateTime? completedFrom,
        DateTime? completedTo,
        string? productNo,
        string? productName,
        string? unit,
        string? requestedByName,
        string? confirmedByName,
        int page,
        int size,
        CancellationToken ct)
    {
        var cmd = new CommandDefinition(
            """
            select o.id, o.order_no, o.warehouse_id, o.status, o.requested_at, o.confirmed_at,
                   o.requested_by, o.requested_by_name, o.confirmed_by, o.confirmed_by_name,
                   count(*) over()::int as total_count
            from outbound_orders o
            where o.status in (@Confirmed, @Rejected)
              and (@Status::smallint is null or o.status = @Status::smallint)
              and (@WarehouseId::uuid is null or o.warehouse_id = @WarehouseId::uuid)
              and (@OrderNo is null or o.order_no ilike '%' || @OrderNo || '%')
              and (@RequestedFrom::timestamp is null or o.requested_at >= @RequestedFrom::timestamp)
              and (@RequestedTo::timestamp is null or o.requested_at <= @RequestedTo::timestamp)
              and (@CompletedFrom::timestamp is null or o.confirmed_at >= @CompletedFrom::timestamp)
              and (@CompletedTo::timestamp is null or o.confirmed_at <= @CompletedTo::timestamp)
              and (@RequestedByName is null or o.requested_by_name ilike '%' || @RequestedByName || '%')
              and (@ConfirmedByName is null or o.confirmed_by_name ilike '%' || @ConfirmedByName || '%')
              and (
                (@ProductNo is null and @ProductName is null and @Unit is null)
                or exists (
                    select 1
                    from outbound_order_items i
                    join products p on p.id = i.product_id
                    where i.outbound_order_id = o.id
                      and (@ProductNo is null or p.product_no ilike '%' || @ProductNo || '%')
                      and (@ProductName is null or p.name ilike '%' || @ProductName || '%')
                      and (@Unit is null or p.unit = @Unit)
                )
              )
            order by o.confirmed_at desc
            limit @Size offset @Offset
            """,
            new
            {
                Confirmed = OutboundOrderStatus.Confirmed,
                Rejected = OutboundOrderStatus.Rejected,
                Status = status,
                WarehouseId = warehouseId,
                OrderNo = orderNo,
                RequestedFrom = requestedFrom,
                RequestedTo = requestedTo,
                CompletedFrom = completedFrom,
                CompletedTo = completedTo,
                RequestedByName = requestedByName,
                ConfirmedByName = confirmedByName,
                ProductNo = productNo,
                ProductName = productName,
                Unit = unit,
                Size = size,
                Offset = (page - 1) * size
            },
            cancellationToken: ct,
            transaction: unitOfWork.Transaction
        );

        var rows = (await unitOfWork.Connection.QueryAsync<HistoryRow>(cmd)).ToList();

        var history = rows.Select(r => new OutboundHistoryDto(
            r.Id, r.OrderNo, r.WarehouseId, ((OutboundOrderStatus)r.Status).ToString(), r.RequestedAt, r.ConfirmedAt,
            r.RequestedBy, r.RequestedByName, r.ConfirmedBy, r.ConfirmedByName));

        var totalCount = rows.Count > 0 ? rows[0].TotalCount : 0;

        return new PagedResult<OutboundHistoryDto>(history.ToList(), totalCount, page, size);
    }

    public async Task<Result<IReadOnlyList<PendingOutboundQuantityDto>>> ListPendingQuantitiesAsync(
        Guid? warehouseId, Guid? productId, CancellationToken ct)
    {
        var cmd = new CommandDefinition(
            """
            select i.product_id, o.warehouse_id, sum(i.quantity)::int as pending_quantity
            from outbound_orders o
            join outbound_order_items i on i.outbound_order_id = o.id
            where o.status = @Status
              and (@WarehouseId::uuid is null or o.warehouse_id = @WarehouseId::uuid)
              and (@ProductId::uuid is null or i.product_id = @ProductId::uuid)
            group by o.warehouse_id, i.product_id
            """,
            new { Status = OutboundOrderStatus.Pending, WarehouseId = warehouseId, ProductId = productId },
            cancellationToken: ct,
            transaction: unitOfWork.Transaction
        );

        var quantities = await unitOfWork.Connection.QueryAsync<PendingOutboundQuantityDto>(cmd);

        return Result.Success<IReadOnlyList<PendingOutboundQuantityDto>>(quantities.ToList());
    }

    // Status 故意存 short,理由跟下面 OrderRow 一樣。
    private sealed record HistoryRow(
        Guid Id,
        string OrderNo,
        Guid WarehouseId,
        short Status,
        DateTime RequestedAt,
        DateTime? ConfirmedAt,
        Guid RequestedBy,
        string RequestedByName,
        Guid? ConfirmedBy,
        string? ConfirmedByName,
        int TotalCount);

    // Status 故意存 short(對應 DB 的 smallint),不是 OutboundOrderStatus enum ——
    // record 沒有無參數建構子,Dapper 不管是單一型別還是 multi-map 查詢,都只能走
    // 建構子完全比對的路徑(不是屬性 setter 賦值那條會自動做數字轉 enum 的路),
    // 型別對不上就直接丟 InvalidOperationException。
    private sealed record OrderRow(
        Guid Id,
        string OrderNo,
        Guid WarehouseId,
        short Status,
        string? RejectReason,
        Guid RequestedBy,
        string RequestedByName,
        DateTime RequestedAt,
        Guid? ConfirmedBy,
        string? ConfirmedByName,
        DateTime? ConfirmedAt);

    private sealed record ItemRow(Guid ProductId, int Quantity);

    private sealed record ProductRow(string ProductNo, string Name, string Unit);
}
