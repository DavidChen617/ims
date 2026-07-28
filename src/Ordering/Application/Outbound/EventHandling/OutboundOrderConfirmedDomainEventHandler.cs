using Application.Abstracts;
using Domain.OutboundOrders.Events;
using MessageContract.OutboundOrders;
using SharedKernel;

namespace Application.Outbound.EventHandling;

public sealed class OutboundOrderConfirmedDomainEventHandler(
    IIntegrationEventWriter writer,
    ICacher cacher
) : IDomainEventHandler<OutboundOrderConfirmedDomainEvent>
{
    public async Task HandleAsync(OutboundOrderConfirmedDomainEvent notification, CancellationToken cancellationToken)
    {
        var integrationEvent = new OutboundOrderConfirmedIntegrationEvent(
            notification.OutboundOrderId, notification.WarehouseId, notification.ConfirmedBy);

        await writer.WriteAsync(integrationEvent, cancellationToken);

        // 除了這個倉庫自己的快取,Admin 沒篩選倉庫的「全倉庫」歷程快取也一併失效。
        await cacher.DeleteByPrefixAsync($"outbound-history:{notification.WarehouseId}", cancellationToken);
        await cacher.DeleteByPrefixAsync($"outbound-history:{HistoryCacheKey.AllWarehouses}", cancellationToken);
    }
}
