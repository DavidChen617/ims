using Domain.InboundOrders.Events;
using MessageContract;
using MessageContract.InboundOrders;
using SharedKernel;

using Application.Abstracts;
namespace Application.Inbound.EventHandling;

public sealed class InboundOrderRejectedDomainEventHandler(
    IIntegrationEventWriter writer,
    ICacher cacher
) : IDomainEventHandler<InboundOrderRejectedDomainEvent>
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

        // 有新的一筆歷程進來,兩種歷程查詢(按品項攤平 / 按訂單彙總)的快取都要失效。
        await cacher.DeleteByPrefixAsync($"inbound-history:{notification.WarehouseId}", cancellationToken);
        await cacher.DeleteByPrefixAsync($"inbound-order-history:{notification.WarehouseId}", cancellationToken);
    }
}
