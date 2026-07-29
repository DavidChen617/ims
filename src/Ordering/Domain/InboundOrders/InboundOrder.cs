using Davish.Result;
using Domain.InboundOrders.Events;
using SharedKernel;

namespace Domain.InboundOrders;

public sealed class InboundOrder : AggregateRoot
{
    private readonly List<InboundOrderItem> _items = new();

    public string OrderNo { get; private set; } = null!;
    public Guid WarehouseId { get; private set; }
    public InboundOrderStatus Status { get; private set; }
    public string? RejectReason { get; private set; }
    public Guid RequestedBy { get; private set; }
    public string RequestedByName { get; private set; } = null!;
    public DateTime RequestedAt { get; private set; }
    public Guid? ConfirmedBy { get; private set; }
    public string? ConfirmedByName { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }
    public IReadOnlyList<InboundOrderItem> Items => _items;

    public static Result<InboundOrder> Create(
        string orderNo,
        Guid warehouseId,
        Guid requestedBy,
        string requestedByName,
        IReadOnlyList<(Guid ProductId, int Quantity, decimal UnitPrice)> items)
    {
        var orderItems = new List<InboundOrderItem>();

        foreach (var item in items)
        {
            if (item.Quantity <= 0)
                return new Error("InboundOrder.Create", "Quantity must be positive");

            var unitPriceResult = UnitPrice.Create(item.UnitPrice);

            if (!unitPriceResult.IsSuccess)
                return unitPriceResult.Error;

            orderItems.Add(new InboundOrderItem(item.ProductId, item.Quantity, unitPriceResult.Value));
        }

        var order = new InboundOrder
        {
            Id = Guid.CreateVersion7(),
            OrderNo = orderNo,
            WarehouseId = warehouseId,
            Status = InboundOrderStatus.Pending,
            RequestedBy = requestedBy,
            RequestedByName = requestedByName,
            RequestedAt = DateTime.UtcNow
        };

        order._items.AddRange(orderItems);

        order.RaiseDomainEvent(new InboundOrderCreatedDomainEvent(
            order.Id,
            warehouseId,
            order._items.Select(item => (item.ProductId, item.Quantity)).ToList()));

        return order;
    }

    public Result Confirm(Guid confirmedBy, string confirmedByName)
    {
        if (!Status.IsPending())
            return new Error("InboundOrder.Confirm", "Only pending orders can be confirmed");

        Status = InboundOrderStatus.Confirmed;
        ConfirmedBy = confirmedBy;
        ConfirmedByName = confirmedByName;
        ConfirmedAt = DateTime.UtcNow;

        RaiseDomainEvent(new InboundOrderConfirmedDomainEvent(Id, WarehouseId, confirmedBy));

        return Result.Success();
    }

    public Result Reject(Guid rejectedBy, string rejectedByName, string reason)
    {
        if (!Status.IsPending())
            return new Error("InboundOrder.Reject", "Only pending orders can be rejected");

        Status = InboundOrderStatus.Rejected;
        ConfirmedBy = rejectedBy;
        ConfirmedByName = rejectedByName;
        ConfirmedAt = DateTime.UtcNow;
        RejectReason = reason;

        RaiseDomainEvent(new InboundOrderRejectedDomainEvent(
            Id,
            WarehouseId,
            rejectedBy,
            reason,
            _items.Select(item => (item.ProductId, item.Quantity)).ToList()));

        return Result.Success();
    }

    public void AttachItems(IEnumerable<InboundOrderItem> items)
    {
        _items.AddRange(items);
    }
}
