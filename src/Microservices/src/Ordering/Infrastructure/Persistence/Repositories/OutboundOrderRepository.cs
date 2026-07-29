using Dapper;
using Davish.Result;
using Domain.OutboundOrders;
using SharedKernel;

namespace Infrastructure.Persistence.Repositories;

public sealed class OutboundOrderRepository(
    IOrderingUnitOfWork unitOfWork,
    IAggregateRootChangeTracker tracker
) : IOutboundOrderRepository
{
    public async Task<Result> AddAsync(OutboundOrder order, CancellationToken ct)
    {
        var cmd = new CommandDefinition(
            """
            insert into outbound_orders(
                id, order_no, warehouse_id, status, reject_reason,
                requested_by, requested_by_name, requested_at,
                confirmed_by, confirmed_by_name, confirmed_at)
            values(
                @Id, @OrderNo, @WarehouseId, @Status, @RejectReason,
                @RequestedBy, @RequestedByName, @RequestedAt,
                @ConfirmedBy, @ConfirmedByName, @ConfirmedAt)
            """,
            order,
            cancellationToken: ct,
            transaction: unitOfWork.Transaction
        );

        await unitOfWork.Connection.ExecuteAsync(cmd);

        var itemsCmd = new CommandDefinition(
            """
            insert into outbound_order_items(outbound_order_id, product_id, quantity)
            values(@OutboundOrderId, @ProductId, @Quantity)
            """,
            order.Items.Select(item => new
            {
                OutboundOrderId = order.Id,
                item.ProductId,
                item.Quantity
            }),
            cancellationToken: ct,
            transaction: unitOfWork.Transaction
        );

        await unitOfWork.Connection.ExecuteAsync(itemsCmd);

        tracker.Enqueue(order);

        return Result.Success();
    }

    public async Task<Result<OutboundOrder>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var cmd = new CommandDefinition(
            """
            select o.id, o.order_no, o.warehouse_id, o.status, o.reject_reason,
                   o.requested_by, o.requested_by_name, o.requested_at,
                   o.confirmed_by, o.confirmed_by_name, o.confirmed_at,
                   i.product_id, i.quantity
            from outbound_orders o
            left join outbound_order_items i on i.outbound_order_id = o.id
            where o.id = @Id
            """,
            new { Id = id },
            cancellationToken: ct,
            transaction: unitOfWork.Transaction
        );

        OutboundOrder? order = null;

        await unitOfWork.Connection.QueryAsync<OutboundOrder, OutboundOrderItem?, OutboundOrder>(
            cmd,
            (o, item) =>
            {
                order ??= o;

                if (item is not null)
                    order.AttachItems([item]);

                return order;
            },
            splitOn: "product_id"
        );

        return order is null
            ? new Error("OutboundOrder.NotFound", "Outbound order not found", ErrorType.NotFound)
            : order;
    }

    public async Task<Result> SaveAsync(OutboundOrder order, CancellationToken ct)
    {
        var cmd = new CommandDefinition(
            """
            update outbound_orders
            set status = @Status,
                reject_reason = @RejectReason,
                confirmed_by = @ConfirmedBy,
                confirmed_by_name = @ConfirmedByName,
                confirmed_at = @ConfirmedAt
            where id = @Id
            """,
            order,
            cancellationToken: ct,
            transaction: unitOfWork.Transaction
        );

        await unitOfWork.Connection.ExecuteAsync(cmd);

        tracker.Enqueue(order);

        return Result.Success();
    }
}
