using Domain.InboundOrders;
using Domain.InboundOrders.Events;

namespace Ordering.UnitTest;

public class InboundOrderTests
{
    private static readonly Guid WarehouseId = Guid.CreateVersion7();
    private static readonly Guid RequestedBy = Guid.CreateVersion7();
    private static readonly Guid ProductId = Guid.CreateVersion7();

    private static IReadOnlyList<(Guid ProductId, int Quantity, decimal UnitPrice)> OneItem()
        => [(ProductId, 10, 5.5m)];

    [Fact]
    public void GivenValidInputs_WhenCreated_ThenStartsAsPendingAndRaisesCreatedEvent()
    {
        var result = InboundOrder.Create("IN-001", WarehouseId, RequestedBy, "Requester", OneItem());

        Assert.True(result.IsSuccess);
        var order = result.Value;
        Assert.Equal(InboundOrderStatus.Pending, order.Status);
        Assert.Equal("IN-001", order.OrderNo);
        Assert.Equal(WarehouseId, order.WarehouseId);
        Assert.Equal(RequestedBy, order.RequestedBy);
        Assert.Single(order.Items);
        Assert.Equal(ProductId, order.Items[0].ProductId);
        Assert.Equal(10, order.Items[0].Quantity);
        Assert.Equal(5.5m, order.Items[0].UnitPrice);

        var raised = Assert.Single(order.DomainEvents);
        var createdEvent = Assert.IsType<InboundOrderCreatedDomainEvent>(raised);
        Assert.Equal(order.Id, createdEvent.InboundOrderId);
        Assert.Equal(WarehouseId, createdEvent.WarehouseId);
    }

    [Fact]
    public void GivenPendingOrder_WhenConfirmed_ThenSucceedsAndSetsConfirmed()
    {
        var order = InboundOrder.Create("IN-001", WarehouseId, RequestedBy, "Requester", OneItem()).Value;
        var confirmedBy = Guid.CreateVersion7();

        var result = order.Confirm(confirmedBy, "Reviewer");

        Assert.True(result.IsSuccess);
        Assert.Equal(InboundOrderStatus.Confirmed, order.Status);
        Assert.Equal(confirmedBy, order.ConfirmedBy);
        Assert.NotNull(order.ConfirmedAt);
    }

    [Fact]
    public void GivenPendingOrder_WhenRejected_ThenSucceedsAndRaisesRejectedEvent()
    {
        var order = InboundOrder.Create("IN-001", WarehouseId, RequestedBy, "Requester", OneItem()).Value;
        var rejectedBy = Guid.CreateVersion7();

        var result = order.Reject(rejectedBy, "Reviewer", "數量與實際不符");

        Assert.True(result.IsSuccess);
        Assert.Equal(InboundOrderStatus.Rejected, order.Status);
        Assert.Equal("數量與實際不符", order.RejectReason);
        Assert.Equal(rejectedBy, order.ConfirmedBy);

        var raised = order.DomainEvents.OfType<InboundOrderRejectedDomainEvent>().Single();
        Assert.Equal(order.Id, raised.InboundOrderId);
        Assert.Equal(rejectedBy, raised.RejectedBy);
        Assert.Equal("數量與實際不符", raised.Reason);
        Assert.Single(raised.Items);
    }

    [Theory]
    [InlineData(InboundOrderStatus.Confirmed)]
    [InlineData(InboundOrderStatus.Rejected)]
    public void GivenAlreadyProcessedOrder_WhenConfirmed_ThenFails(InboundOrderStatus status)
    {
        var order = InboundOrder.Create("IN-001", WarehouseId, RequestedBy, "Requester", OneItem()).Value;
        MoveToStatus(order, status);

        var result = order.Confirm(Guid.CreateVersion7(), "Reviewer");

        Assert.False(result.IsSuccess);
    }

    [Theory]
    [InlineData(InboundOrderStatus.Confirmed)]
    [InlineData(InboundOrderStatus.Rejected)]
    public void GivenAlreadyProcessedOrder_WhenRejected_ThenFails(InboundOrderStatus status)
    {
        var order = InboundOrder.Create("IN-001", WarehouseId, RequestedBy, "Requester", OneItem()).Value;
        MoveToStatus(order, status);

        var result = order.Reject(Guid.CreateVersion7(), "Reviewer", "any reason");

        Assert.False(result.IsSuccess);
    }

    private static void MoveToStatus(InboundOrder order, InboundOrderStatus status)
    {
        switch (status)
        {
            case InboundOrderStatus.Confirmed:
                order.Confirm(Guid.CreateVersion7(), "Reviewer");
                break;
            case InboundOrderStatus.Rejected:
                order.Reject(Guid.CreateVersion7(), "Reviewer", "setup");
                break;
        }
    }
}
