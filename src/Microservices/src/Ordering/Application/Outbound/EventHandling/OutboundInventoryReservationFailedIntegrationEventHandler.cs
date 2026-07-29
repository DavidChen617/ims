using Domain.OutboundOrders;
using MessageContract.OutboundOrders;
using Microsoft.Extensions.Logging;

namespace Application.Outbound.EventHandling;

public sealed class OutboundInventoryReservationFailedIntegrationEventHandler(
    IOutboundOrderRepository repository,
    ILogger<OutboundInventoryReservationFailedIntegrationEventHandler> logger
) : MessageContract.IIntegrationEventHandler<OutboundInventoryReservationFailedIntegrationEvent>
{
    public async Task HandleAsync(
        OutboundInventoryReservationFailedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var orderResult = await repository.GetByIdAsync(notification.OutboundOrderId, cancellationToken);

        if (!orderResult.IsSuccess)
        {
            logger.LogError(
                "OutboundInventoryReservationFailedIntegrationEvent: {EventId} received but order {OutboundId} does not exist.",
                notification.Id, notification.OutboundOrderId);
            return;
        }

        var failResult = orderResult.Value.FailReservation("庫存不足,系統自動拒絕");

        if (!failResult.IsSuccess)
        {
            logger.LogWarning(
                "OutboundInventoryReservationFailedIntegrationEvent: {EventId} received but order {OutboundId} could not fail reservation: {Error}",
                notification.Id, notification.OutboundOrderId, failResult.Error.Description);
            return;
        }

        await repository.SaveAsync(orderResult.Value, cancellationToken);
    }
}
