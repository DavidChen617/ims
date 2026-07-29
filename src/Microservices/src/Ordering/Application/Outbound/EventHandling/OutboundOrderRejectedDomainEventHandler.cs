using Domain.OutboundOrders.Events;
using MessageContract;
using MessageContract.OutboundOrders;
using SharedKernel;

using Application.Abstracts;
namespace Application.Outbound.EventHandling;

public sealed class OutboundOrderRejectedDomainEventHandler(
    IIntegrationEventWriter writer,
    ICacher cacher
) : IDomainEventHandler<OutboundOrderRejectedDomainEvent>
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

        // 除了這個倉庫自己的快取,Admin 沒篩選倉庫的「全倉庫」歷程快取也一併失效。
        await cacher.DeleteByPrefixAsync($"outbound-history:{notification.WarehouseId}", cancellationToken);
        await cacher.DeleteByPrefixAsync($"outbound-history:{HistoryCacheKey.AllWarehouses}", cancellationToken);
    }
}
