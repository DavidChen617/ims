using Domain.OutboundOrders;
using MessageContract.OutboundOrders;
using Microsoft.Extensions.Logging;

namespace Application.Outbound.EventHandling;

public sealed class OutboundInventoryReservedIntegrationEventHandler(
    IOutboundOrderRepository repository,
    ILogger<OutboundInventoryReservedIntegrationEventHandler> logger
) : MessageContract.IIntegrationEventHandler<OutboundInventoryReservedIntegrationEvent>
{
    public async Task HandleAsync(
        OutboundInventoryReservedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var orderResult = await repository.GetByIdAsync(notification.OutboundOrderId, cancellationToken);

        if (!orderResult.IsSuccess)
        {
            logger.LogError("OutboundInventoryReservedIntegrationEvent: {EventId} received But Order with id {OutboundId} does not exist.", notification.Id, notification.OutboundOrderId);
            return;
        }

        var markResult = orderResult.Value.MarkReserved();

        if (!markResult.IsSuccess)
        {
            logger.LogWarning(
                "OutboundInventoryReservedIntegrationEvent: {EventId} received but order {OutboundId} could not be marked as reserved: {Error}",
                notification.Id, notification.OutboundOrderId, markResult.Error.Description);
            return;
        }

        await repository.SaveAsync(orderResult.Value, cancellationToken);
    }
}
