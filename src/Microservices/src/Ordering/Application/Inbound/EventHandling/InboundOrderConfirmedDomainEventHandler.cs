using Application.Abstracts;
using Domain.InboundOrders.Events;
using Domain.Products;
using MessageContract;
using MessageContract.InboundOrders;
using SharedKernel;

namespace Application.Inbound.EventHandling;

public sealed class InboundOrderConfirmedDomainEventHandler(
    IIntegrationEventWriter writer,
    ICacher cacher,
    IProductRepository productRepository,
    ICurrentUser currentUser
) : IDomainEventHandler<InboundOrderConfirmedDomainEvent>
{
    public async Task HandleAsync(InboundOrderConfirmedDomainEvent notification, CancellationToken cancellationToken)
    {
        var productIds = notification.Items.Select(i => i.ProductId).ToList();
        var products = (await productRepository.GetByIdsAsync(productIds, cancellationToken)).Value
            .ToDictionary(p => p.Id);

        var integrationEvent = new InboundOrderConfirmedIntegrationEvent(
            notification.InboundOrderId,
            notification.WarehouseId,
            currentUser.WarehouseName,
            notification.ConfirmedBy,
            notification.Items
                .Select(item =>
                {
                    var product = products[item.ProductId];
                    return new EnrichedOrderItem(item.ProductId, product.ProductNo, product.Name, product.Unit.Name, item.Quantity);
                })
                .ToList());

        await writer.WriteAsync(integrationEvent, cancellationToken);

        // 有新的一筆歷程進來,兩種歷程查詢(按品項攤平 / 按訂單彙總)的快取都要失效。
        await cacher.DeleteByPrefixAsync($"inbound-history:{notification.WarehouseId}", cancellationToken);
        await cacher.DeleteByPrefixAsync($"inbound-order-history:{notification.WarehouseId}", cancellationToken);
    }
}
