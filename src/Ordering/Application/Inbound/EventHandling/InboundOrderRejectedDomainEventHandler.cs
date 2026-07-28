using Domain.InboundOrders.Events;
using MessageContract;
using MessageContract.InboundOrders;
using SharedKernel;

namespace Application.Inbound.EventHandling;

public sealed class InboundOrderRejectedDomainEventHandler(IIntegrationEventWriter writer)
    : IDomainEventHandler<InboundOrderRejectedDomainEvent>
{
    public async Task HandleAsync(InboundOrderRejectedDomainEvent notification, CancellationToken cancellationToken)
    {
        var integrationEvent = new InboundOrderRejectedIntegrationEvent(
            notification.InboundOrderId,
            notification.WarehouseId,
            notification.RejectedBy,
            notification.Reason,
            notification.Items.Select(i => new OrderItem(i.ProductId, i.Quantity)).ToList());

        await writer.WriteAsync(integrationEvent, cancellationToken);
    }
}
