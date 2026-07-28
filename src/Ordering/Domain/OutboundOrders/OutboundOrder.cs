using Davish.Result;
using Domain.OutboundOrders.Events;
using SharedKernel;

namespace Domain.OutboundOrders;

public sealed class OutboundOrder : AggregateRoot
{
    private readonly List<OutboundOrderItem> _items = new();

    public string OrderNo { get; private set; } = null!;
    public Guid WarehouseId { get; private set; }
    public OutboundOrderStatus Status { get; private set; }
    public string? RejectReason { get; private set; }
    public Guid RequestedBy { get; private set; }
    public string RequestedByName { get; private set; } = null!;
    public DateTime RequestedAt { get; private set; }
    public Guid? ConfirmedBy { get; private set; }
    public string? ConfirmedByName { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }
    public IReadOnlyList<OutboundOrderItem> Items => _items;

    public static Result<OutboundOrder> Create(
        string orderNo,
        Guid warehouseId,
        Guid requestedBy,
        string requestedByName,
        IReadOnlyList<(Guid ProductId, int Quantity)> items)
    {
        var order = new OutboundOrder
        {
            Id = Guid.CreateVersion7(),
            OrderNo = orderNo,
            WarehouseId = warehouseId,
            Status = OutboundOrderStatus.Processing,
            RequestedBy = requestedBy,
            RequestedByName = requestedByName,
            RequestedAt = DateTime.UtcNow
        };

        order._items.AddRange(items.Select(item => new OutboundOrderItem(item.ProductId, item.Quantity)));

        order.RaiseDomainEvent(new OutboundOrderCreatedDomainEvent(
            order.Id,
            warehouseId,
            order._items.Select(item => (item.ProductId, item.Quantity)).ToList()));

        return order;
    }

    public Result MarkReserved()
    {
        if (!Status.IsProcessing())
            return new Error("OutboundOrder.MarkReserved", "Only processing orders can be marked as reserved");

        Status = OutboundOrderStatus.Pending;

        return Result.Success();
    }

    public Result FailReservation(string reason)
    {
        if (!Status.IsProcessing())
            return new Error("OutboundOrder.FailReservation", "Only processing orders can fail reservation");

        Status = OutboundOrderStatus.Rejected;
        RejectReason = reason;
        ConfirmedAt = DateTime.UtcNow;

        return Result.Success();
    }

    public Result Confirm(Guid confirmedBy, string confirmedByName)
    {
        if (!Status.IsPending())
            return new Error("OutboundOrder.Confirm", "Only pending orders can be confirmed");

        Status = OutboundOrderStatus.Confirmed;
        ConfirmedBy = confirmedBy;
        ConfirmedByName = confirmedByName;
        ConfirmedAt = DateTime.UtcNow;

        RaiseDomainEvent(new OutboundOrderConfirmedDomainEvent(Id, WarehouseId, confirmedBy));

        return Result.Success();
    }

    public Result Reject(Guid confirmedBy, string confirmedByName, string reason)
    {
        if (!Status.IsPending())
            return new Error("OutboundOrder.Reject", "Only pending orders can be rejected");

        Status = OutboundOrderStatus.Rejected;
        ConfirmedBy = confirmedBy;
        ConfirmedByName = confirmedByName;
        ConfirmedAt = DateTime.UtcNow;
        RejectReason = reason;

        RaiseDomainEvent(new OutboundOrderRejectedDomainEvent(
            Id,
            WarehouseId,
            confirmedBy,
            reason,
            _items.Select(item => (item.ProductId, item.Quantity)).ToList()));

        return Result.Success();
    }

    public void AttachItems(IEnumerable<OutboundOrderItem> items)
    {
        _items.AddRange(items);
    }
}

public sealed class OutboundOrderItem : Entity<int>
{
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }

    private OutboundOrderItem()
    {
    }

    internal OutboundOrderItem(Guid productId, int quantity)
    {
        ProductId = productId;
        Quantity = quantity;
    }
}
