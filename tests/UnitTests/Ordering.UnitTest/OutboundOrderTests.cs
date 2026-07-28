using Domain.OutboundOrders;
using Domain.OutboundOrders.Events;

namespace Ordering.UnitTest;

public class OutboundOrderTests
{
    private static readonly Guid WarehouseId = Guid.CreateVersion7();
    private static readonly Guid RequestedBy = Guid.CreateVersion7();
    private static readonly Guid ProductId = Guid.CreateVersion7();

    private static IReadOnlyList<(Guid ProductId, int Quantity)> OneItem()
        => [(ProductId, 3)];

    [Fact]
    public void GivenValidInputs_WhenCreated_ThenStartsAsProcessingAndRaisesCreatedEvent()
    {
        var result = OutboundOrder.Create("OUT-001", WarehouseId, RequestedBy, "Requester", OneItem());

        Assert.True(result.IsSuccess);
        var order = result.Value;
        Assert.Equal(OutboundOrderStatus.Processing, order.Status);
        Assert.Equal("OUT-001", order.OrderNo);
        Assert.Equal(WarehouseId, order.WarehouseId);
        Assert.Single(order.Items);
        Assert.Equal(ProductId, order.Items[0].ProductId);
        Assert.Equal(3, order.Items[0].Quantity);

        var raised = Assert.Single(order.DomainEvents);
        var createdEvent = Assert.IsType<OutboundOrderCreatedDomainEvent>(raised);
        Assert.Equal(order.Id, createdEvent.OutboundOrderId);
        Assert.Equal(WarehouseId, createdEvent.WarehouseId);
    }

    [Fact]
    public void GivenProcessingOrder_WhenMarkedReserved_ThenSucceedsAndBecomesPending()
    {
        var order = CreateOrder();

        var result = order.MarkReserved();

        Assert.True(result.IsSuccess);
        Assert.Equal(OutboundOrderStatus.Pending, order.Status);
    }

    [Fact]
    public void GivenProcessingOrder_WhenReservationFails_ThenBecomesRejectedWithoutPassingThroughPending()
    {
        var order = CreateOrder();

        var result = order.FailReservation("庫存不足");

        Assert.True(result.IsSuccess);
        Assert.Equal(OutboundOrderStatus.Rejected, order.Status);
        Assert.Equal("庫存不足", order.RejectReason);
        Assert.NotNull(order.ConfirmedAt);
    }

    [Fact]
    public void GivenPendingOrder_WhenConfirmed_ThenSucceeds()
    {
        var order = CreateOrder();
        order.MarkReserved();
        var confirmedBy = Guid.CreateVersion7();

        var result = order.Confirm(confirmedBy, "Reviewer");

        Assert.True(result.IsSuccess);
        Assert.Equal(OutboundOrderStatus.Confirmed, order.Status);
        Assert.Equal(confirmedBy, order.ConfirmedBy);
    }

    [Fact]
    public void GivenPendingOrder_WhenRejectedByAdmin_ThenSucceedsAndRaisesRejectedEvent()
    {
        var order = CreateOrder();
        order.MarkReserved();
        var rejectedBy = Guid.CreateVersion7();

        var result = order.Reject(rejectedBy, "Reviewer", "客戶取消訂單");

        Assert.True(result.IsSuccess);
        Assert.Equal(OutboundOrderStatus.Rejected, order.Status);
        Assert.Equal("客戶取消訂單", order.RejectReason);

        var raised = order.DomainEvents.OfType<OutboundOrderRejectedDomainEvent>().Single();
        Assert.Equal(order.Id, raised.OutboundOrderId);
        Assert.Equal(rejectedBy, raised.RejectedBy);
        Assert.Single(raised.Items);
    }

    [Theory]
    [InlineData(OutboundOrderStatus.Pending)]
    [InlineData(OutboundOrderStatus.Confirmed)]
    [InlineData(OutboundOrderStatus.Rejected)]
    public void GivenNonProcessingOrder_WhenMarkedReserved_ThenFails(OutboundOrderStatus status)
    {
        var order = MoveToStatus(status);

        var result = order.MarkReserved();

        Assert.False(result.IsSuccess);
    }

    [Theory]
    [InlineData(OutboundOrderStatus.Pending)]
    [InlineData(OutboundOrderStatus.Confirmed)]
    [InlineData(OutboundOrderStatus.Rejected)]
    public void GivenNonProcessingOrder_WhenReservationFails_ThenFails(OutboundOrderStatus status)
    {
        var order = MoveToStatus(status);

        var result = order.FailReservation("庫存不足");

        Assert.False(result.IsSuccess);
    }

    [Theory]
    [InlineData(OutboundOrderStatus.Processing)]
    [InlineData(OutboundOrderStatus.Confirmed)]
    [InlineData(OutboundOrderStatus.Rejected)]
    public void GivenNonPendingOrder_WhenConfirmed_ThenFails(OutboundOrderStatus status)
    {
        var order = MoveToStatus(status);

        var result = order.Confirm(Guid.CreateVersion7(), "Reviewer");

        Assert.False(result.IsSuccess);
    }

    [Theory]
    [InlineData(OutboundOrderStatus.Processing)]
    [InlineData(OutboundOrderStatus.Confirmed)]
    [InlineData(OutboundOrderStatus.Rejected)]
    public void GivenNonPendingOrder_WhenRejectedByAdmin_ThenFails(OutboundOrderStatus status)
    {
        var order = MoveToStatus(status);

        var result = order.Reject(Guid.CreateVersion7(), "Reviewer", "any reason");

        Assert.False(result.IsSuccess);
    }

    private static OutboundOrder CreateOrder()
        => OutboundOrder.Create("OUT-001", WarehouseId, RequestedBy, "Requester", OneItem()).Value;

    private static OutboundOrder MoveToStatus(OutboundOrderStatus status)
    {
        var order = CreateOrder();

        switch (status)
        {
            case OutboundOrderStatus.Processing:
                break;
            case OutboundOrderStatus.Pending:
                order.MarkReserved();
                break;
            case OutboundOrderStatus.Confirmed:
                order.MarkReserved();
                order.Confirm(Guid.CreateVersion7(), "Reviewer");
                break;
            case OutboundOrderStatus.Rejected:
                order.MarkReserved();
                order.Reject(Guid.CreateVersion7(), "Reviewer", "setup");
                break;
        }

        return order;
    }
}
