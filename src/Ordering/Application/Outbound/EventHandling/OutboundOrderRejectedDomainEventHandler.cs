using Domain.OutboundOrders.Events;
using MessageContract;
using MessageContract.OutboundOrders;
using SharedKernel;

namespace Application.Outbound.EventHandling;

public sealed class OutboundOrderRejectedDomainEventHandler(IIntegrationEventWriter writer)
    : IDomainEventHandler<OutboundOrderRejectedDomainEvent>
{
    public async Task HandleAsync(OutboundOrderRejectedDomainEvent notification, CancellationToken cancellationToken)
    {
        var integrationEvent = new OutboundOrderRejectedIntegrationEvent(
            notification.OutboundOrderId,
            notification.WarehouseId,
            notification.RejectedBy,
            notification.Reason,
            notification.Items.Select(i => new OrderItem(i.ProductId, i.Quantity)).ToList());

        await writer.WriteAsync(integrationEvent, cancellationToken);
    }
}
