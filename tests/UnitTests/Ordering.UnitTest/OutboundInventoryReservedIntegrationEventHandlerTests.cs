using Application.Outbound.EventHandling;
using Domain.OutboundOrders;
using MessageContract.OutboundOrders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Ordering.UnitTest.Fakes;

namespace Ordering.UnitTest;

public class OutboundInventoryReservedIntegrationEventHandlerTests
{
    private static readonly Guid WarehouseId = Guid.CreateVersion7();
    private static readonly Guid ProductId = Guid.CreateVersion7();

    private static OutboundOrder CreateOrder()
        => OutboundOrder.Create("OUT-001", WarehouseId, Guid.CreateVersion7(), "Requester", [(ProductId, 1)]).Value;

    [Fact]
    public async Task GivenOrderDoesNotExist_WhenHandled_ThenLogsErrorAndDoesNotSave()
    {
        var repository = new FakeOutboundOrderRepository();
        var logger = new FakeLogger<OutboundInventoryReservedIntegrationEventHandler>();
        var handler = new OutboundInventoryReservedIntegrationEventHandler(repository, logger);
        var missingOrderId = Guid.CreateVersion7();

        await handler.HandleAsync(
            new OutboundInventoryReservedIntegrationEvent(missingOrderId, WarehouseId), CancellationToken.None);

        Assert.Null(repository.Saved);
        Assert.Equal(LogLevel.Error, logger.Collector.LatestRecord.Level);
    }

    [Fact]
    public async Task GivenProcessingOrder_WhenHandled_ThenMarksReservedAndSaves()
    {
        var order = CreateOrder();
        var repository = new FakeOutboundOrderRepository { OrderToReturn = order };
        var logger = new FakeLogger<OutboundInventoryReservedIntegrationEventHandler>();
        var handler = new OutboundInventoryReservedIntegrationEventHandler(repository, logger);

        await handler.HandleAsync(
            new OutboundInventoryReservedIntegrationEvent(order.Id, WarehouseId), CancellationToken.None);

        Assert.Equal(OutboundOrderStatus.Pending, order.Status);
        Assert.Same(order, repository.Saved);
    }

    [Fact]
    public async Task GivenOrderAlreadyPastProcessing_WhenHandled_ThenLogsWarningAndDoesNotSave()
    {
        var order = CreateOrder();
        order.MarkReserved();
        var repository = new FakeOutboundOrderRepository { OrderToReturn = order };
        var logger = new FakeLogger<OutboundInventoryReservedIntegrationEventHandler>();
        var handler = new OutboundInventoryReservedIntegrationEventHandler(repository, logger);

        await handler.HandleAsync(
            new OutboundInventoryReservedIntegrationEvent(order.Id, WarehouseId), CancellationToken.None);

        Assert.Null(repository.Saved);
        Assert.Equal(LogLevel.Warning, logger.Collector.LatestRecord.Level);
    }
}
