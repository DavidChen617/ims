using Application.Abstracts;
using Domain.InboundOrders.Events;
using MessageContract.InboundOrders;
using SharedKernel;

namespace Application.Inbound.EventHandling;

public sealed class InboundOrderConfirmedDomainEventHandler(
    IIntegrationEventWriter writer,
    ICacher cacher
) : IDomainEventHandler<InboundOrderConfirmedDomainEvent>
{
    public async Task HandleAsync(InboundOrderConfirmedDomainEvent notification, CancellationToken cancellationToken)
    {
        var integrationEvent = new InboundOrderConfirmedIntegrationEvent(
            notification.InboundOrderId, notification.WarehouseId, notification.ConfirmedBy);

        await writer.WriteAsync(integrationEvent, cancellationToken);

        // 有新的一筆歷程進來,兩種歷程查詢(按品項攤平 / 按訂單彙總)的快取都要失效。
        await cacher.DeleteByPrefixAsync($"inbound-history:{notification.WarehouseId}", cancellationToken);
        await cacher.DeleteByPrefixAsync($"inbound-order-history:{notification.WarehouseId}", cancellationToken);
    }
}
