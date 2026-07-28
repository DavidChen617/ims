using Domain.OutboundOrders.Events;
using Domain.Products;
using MessageContract;
using MessageContract.OutboundOrders;
using SharedKernel;

using Application.Abstracts;
namespace Application.Outbound.EventHandling;

public sealed class OutboundOrderCreatedDomainEventHandler(
    IIntegrationEventWriter writer,
    IProductRepository productRepository,
    ICurrentUser currentUser
) : IDomainEventHandler<OutboundOrderCreatedDomainEvent>
{
    public async Task HandleAsync(OutboundOrderCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        var productIds = notification.Items.Select(i => i.ProductId).ToList();
        var products = (await productRepository.GetByIdsAsync(productIds, cancellationToken)).Value
            .ToDictionary(p => p.Id);

        var integrationEvent = new OutboundOrderCreatedIntegrationEvent(
            notification.OutboundOrderId,
            notification.WarehouseId,
            currentUser.WarehouseName,
            notification.Items
                .Select(item =>
                {
                    var product = products[item.ProductId];
                    return new EnrichedOrderItem(item.ProductId, product.ProductNo, product.Name, product.Unit.Name, item.Quantity);
                })
                .ToList());

        await writer.WriteAsync(integrationEvent, cancellationToken);
    }
}
