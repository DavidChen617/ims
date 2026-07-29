using Application;
using Application.Inbound;
using Dapper;
using Davish.Result;
using Domain.InboundOrders;

namespace Infrastructure.Persistence.Readers;

public sealed class InboundOrderReader(IOrderingUnitOfWork unitOfWork) : IInboundOrderReader
{
    public Task<Result<InboundOrderDto>> GetByIdAsync(Guid id, CancellationToken ct)
        => GetByIdCoreAsync(id, null, ct);

    public Task<Result<InboundOrderDto>> GetByIdAsync(Guid id, Guid warehouseId, CancellationToken ct)
        => GetByIdCoreAsync(id, warehouseId, ct);

    private async Task<Result<InboundOrderDto>> GetByIdCoreAsync(Guid id, Guid? warehouseId, CancellationToken ct)
    {
        var cmd = new CommandDefinition(
            """
            select o.id, o.order_no, o.warehouse_id, o.status, o.reject_reason,
                   o.requested_by, o.requested_by_name, o.requested_at,
                   o.confirmed_by, o.confirmed_by_name, o.confirmed_at,
                   i.product_id, i.quantity, i.unit_price,
                   p.product_no, p.name, p.unit
            from inbound_orders o
            left join inbound_order_items i on i.inbound_order_id = o.id
            left join products p on p.id = i.product_id
            where o.id = @Id and (@WarehouseId is null or o.warehouse_id = @WarehouseId)
            """,
            new { Id = id, WarehouseId = warehouseId },
            cancellationToken: ct,
            transaction: unitOfWork.Transaction
        );

        OrderRow? header = null;
        var items = new List<InboundOrderItemDto>();

        await unitOfWork.Connection.QueryAsync<OrderRow, ItemRow, ProductRow, OrderRow>(
            cmd,
            (row, item, product) =>
            {
                header ??= row;

                if (item is not null)
                    items.Add(new InboundOrderItemDto(
                        item.ProductId,
                        product?.ProductNo ?? string.Empty,
                        product?.Name ?? string.Empty,
                        product?.Unit ?? string.Empty,
                        item.Quantity,
                        item.UnitPrice,
                        item.Quantity * item.UnitPrice));

                return row;
            },
            splitOn: "product_id,product_no"
        );

        if (header is null)
            return new Error("InboundOrder.NotFound", "Inbound order not found", ErrorType.NotFound);

        return new InboundOrderDto(
            header.Id,
            header.OrderNo,
            header.WarehouseId,
            ((InboundOrderStatus)header.Status).ToString(),
            header.RejectReason,
            header.RequestedBy,
            header.RequestedByName,
            header.RequestedAt,
            header.ConfirmedBy,
            header.ConfirmedByName,
            header.ConfirmedAt,
            items);
    }

    public async Task<Result<PagedResult<PendingInboundOrderDto>>> ListPendingAsync(
        Guid warehouseId, int page, int size, CancellationToken ct)
    {
        var cmd = new CommandDefinition(
            """
            select id, order_no, count(*) over()::int as total_count
            from inbound_orders
            where warehouse_id = @WarehouseId and status = @Status
            order by requested_at
            limit @Size offset @Offset
            """,
            new { WarehouseId = warehouseId, Status = InboundOrderStatus.Pending, Size = size, Offset = (page - 1) * size },
            cancellationToken: ct,
            transaction: unitOfWork.Transaction
        );

        var rows = (await unitOfWork.Connection.QueryAsync<PendingRow>(cmd)).ToList();
        var items = rows.Select(r => new PendingInboundOrderDto(r.Id, r.OrderNo)).ToList();
        var totalCount = rows.Count > 0 ? rows[0].TotalCount : 0;

        return new PagedResult<PendingInboundOrderDto>(items, totalCount, page, size);
    }

    private sealed record PendingRow(Guid Id, string OrderNo, int TotalCount);

    public async Task<Result<InboundHistoryResultDto>> ListHistoryAsync(
        Guid? warehouseId,
        string? orderNo,
        string? productNo,
        string? productName,
        Guid? requestedBy,
        Guid? confirmedBy,
        InboundOrderStatus? status,
        DateTime? requestedFrom,
        DateTime? requestedTo,
        int? quantityMin,
        int? quantityMax,
        decimal? unitPriceMin,
        decimal? unitPriceMax,
        decimal? amountMin,
        decimal? amountMax,
        int page,
        int size,
        CancellationToken ct)
    {
        var cmd = new CommandDefinition(
            """
            with order_totals as (
                select inbound_order_id, sum(quantity * unit_price) as order_amount
                from inbound_order_items
                group by inbound_order_id
            )
            select o.order_no, i.product_id, p.product_no, p.name as product_name, i.quantity, i.unit_price,
                   count(*) over()::int as total_count,
                   coalesce(sum(i.quantity) over(), 0)::int as total_quantity,
                   coalesce(sum(i.quantity * i.unit_price) over(), 0) as total_amount
            from inbound_order_items i
            join inbound_orders o on o.id = i.inbound_order_id
            join products p on p.id = i.product_id
            join order_totals ot on ot.inbound_order_id = o.id
            where o.status in (@Confirmed, @Rejected)
              and (@Status::smallint is null or o.status = @Status::smallint)
              and (@WarehouseId::uuid is null or o.warehouse_id = @WarehouseId::uuid)
              and (@OrderNo is null or o.order_no ilike '%' || @OrderNo || '%')
              and (@RequestedFrom::timestamp is null or o.requested_at >= @RequestedFrom::timestamp)
              and (@RequestedTo::timestamp is null or o.requested_at <= @RequestedTo::timestamp)
              and (@RequestedBy::uuid is null or o.requested_by = @RequestedBy::uuid)
              and (@ConfirmedBy::uuid is null or o.confirmed_by = @ConfirmedBy::uuid)
              and (@ProductNo is null or p.product_no ilike '%' || @ProductNo || '%')
              and (@ProductName is null or p.name ilike '%' || @ProductName || '%')
              and (@QuantityMin::int is null or i.quantity >= @QuantityMin::int)
              and (@QuantityMax::int is null or i.quantity <= @QuantityMax::int)
              and (@UnitPriceMin::numeric is null or i.unit_price >= @UnitPriceMin::numeric)
              and (@UnitPriceMax::numeric is null or i.unit_price <= @UnitPriceMax::numeric)
              and (@AmountMin::numeric is null or ot.order_amount >= @AmountMin::numeric)
              and (@AmountMax::numeric is null or ot.order_amount <= @AmountMax::numeric)
            order by o.requested_at desc, i.id
            limit @Size offset @Offset
            """,
            new
            {
                Confirmed = InboundOrderStatus.Confirmed,
                Rejected = InboundOrderStatus.Rejected,
                Status = status,
                WarehouseId = warehouseId,
                OrderNo = orderNo,
                RequestedFrom = requestedFrom,
                RequestedTo = requestedTo,
                RequestedBy = requestedBy,
                ConfirmedBy = confirmedBy,
                ProductNo = productNo,
                ProductName = productName,
                QuantityMin = quantityMin,
                QuantityMax = quantityMax,
                UnitPriceMin = unitPriceMin,
                UnitPriceMax = unitPriceMax,
                AmountMin = amountMin,
                AmountMax = amountMax,
                Size = size,
                Offset = (page - 1) * size
            },
            cancellationToken: ct,
            transaction: unitOfWork.Transaction
        );

        var rows = (await unitOfWork.Connection.QueryAsync<HistoryRow>(cmd)).ToList();

        var lines = rows
            .Select(r => new InboundHistoryLineDto(
                r.OrderNo, r.ProductId, r.ProductNo, r.ProductName, r.Quantity, r.UnitPrice,
                r.Quantity * r.UnitPrice))
            .ToList();

        var totalCount = rows.Count > 0 ? rows[0].TotalCount : 0;
        var totalQuantity = rows.Count > 0 ? rows[0].TotalQuantity : 0;
        var totalAmount = rows.Count > 0 ? rows[0].TotalAmount : 0m;

        return new InboundHistoryResultDto(
            new PagedResult<InboundHistoryLineDto>(lines, totalCount, page, size),
            totalQuantity,
            totalAmount);
    }

    public async Task<Result<IReadOnlyList<InboundOrderHistoryDto>>> ListOrderHistoryAsync(
        Guid warehouseId,
        InboundOrderStatus? status,
        string? productNo,
        string? productName,
        Guid? requestedBy,
        Guid? confirmedBy,
        DateTime? requestedFrom,
        DateTime? requestedTo,
        DateTime? completedFrom,
        DateTime? completedTo,
        decimal? amountMin,
        decimal? amountMax,
        CancellationToken ct)
    {
        var cmd = new CommandDefinition(
            """
            with order_totals as (
                select inbound_order_id, sum(quantity * unit_price) as order_amount
                from inbound_order_items
                group by inbound_order_id
            )
            select o.id, o.order_no, o.warehouse_id, o.status, o.reject_reason,
                   o.requested_at, o.requested_by, o.requested_by_name,
                   o.confirmed_at, o.confirmed_by, o.confirmed_by_name,
                   coalesce(ot.order_amount, 0) as total_amount
            from inbound_orders o
            left join order_totals ot on ot.inbound_order_id = o.id
            where o.warehouse_id = @WarehouseId
              and o.status in (@Confirmed, @Rejected)
              and (@Status::smallint is null or o.status = @Status::smallint)
              and (@RequestedFrom::timestamp is null or o.requested_at >= @RequestedFrom::timestamp)
              and (@RequestedTo::timestamp is null or o.requested_at <= @RequestedTo::timestamp)
              and (@CompletedFrom::timestamp is null or o.confirmed_at >= @CompletedFrom::timestamp)
              and (@CompletedTo::timestamp is null or o.confirmed_at <= @CompletedTo::timestamp)
              and (@RequestedBy::uuid is null or o.requested_by = @RequestedBy::uuid)
              and (@ConfirmedBy::uuid is null or o.confirmed_by = @ConfirmedBy::uuid)
              and (@AmountMin::numeric is null or ot.order_amount >= @AmountMin::numeric)
              and (@AmountMax::numeric is null or ot.order_amount <= @AmountMax::numeric)
              and (
                (@ProductNo is null and @ProductName is null)
                or exists (
                    select 1
                    from inbound_order_items i
                    join products p on p.id = i.product_id
                    where i.inbound_order_id = o.id
                      and (@ProductNo is null or p.product_no = @ProductNo)
                      and (@ProductName is null or p.name ilike '%' || @ProductName || '%')
                )
              )
            order by o.requested_at desc
            """,
            new
            {
                WarehouseId = warehouseId,
                Confirmed = InboundOrderStatus.Confirmed,
                Rejected = InboundOrderStatus.Rejected,
                Status = status,
                RequestedFrom = requestedFrom,
                RequestedTo = requestedTo,
                CompletedFrom = completedFrom,
                CompletedTo = completedTo,
                RequestedBy = requestedBy,
                ConfirmedBy = confirmedBy,
                AmountMin = amountMin,
                AmountMax = amountMax,
                ProductNo = productNo,
                ProductName = productName
            },
            cancellationToken: ct,
            transaction: unitOfWork.Transaction
        );

        var rows = await unitOfWork.Connection.QueryAsync<OrderHistoryRow>(cmd);

        var history = rows.Select(r => new InboundOrderHistoryDto(
            r.Id, r.OrderNo, r.WarehouseId, ((InboundOrderStatus)r.Status).ToString(), r.RejectReason,
            r.RequestedAt, r.RequestedBy, r.RequestedByName,
            r.ConfirmedAt, r.ConfirmedBy, r.ConfirmedByName, r.TotalAmount));

        return Result.Success<IReadOnlyList<InboundOrderHistoryDto>>(history.ToList());
    }

    private sealed record OrderHistoryRow(
        Guid Id,
        string OrderNo,
        Guid WarehouseId,
        short Status,
        string? RejectReason,
        DateTime RequestedAt,
        Guid RequestedBy,
        string RequestedByName,
        DateTime? ConfirmedAt,
        Guid? ConfirmedBy,
        string? ConfirmedByName,
        decimal TotalAmount);

    private sealed record HistoryRow(
        string OrderNo,
        Guid ProductId,
        string ProductNo,
        string ProductName,
        int Quantity,
        decimal UnitPrice,
        int TotalCount,
        int TotalQuantity,
        decimal TotalAmount);

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

    private sealed record ItemRow(Guid ProductId, int Quantity, decimal UnitPrice);

    private sealed record ProductRow(string ProductNo, string Name, string Unit);
}
