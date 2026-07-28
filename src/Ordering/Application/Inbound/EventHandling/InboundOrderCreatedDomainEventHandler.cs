using Domain.InboundOrders.Events;
using Domain.Products;
using MessageContract;
using MessageContract.InboundOrders;
using SharedKernel;

using Application.Abstracts;
namespace Application.Inbound.EventHandling;

public sealed class InboundOrderCreatedDomainEventHandler(
    IIntegrationEventWriter writer,
    IProductRepository productRepository,
    ICurrentUser currentUser
) : IDomainEventHandler<InboundOrderCreatedDomainEvent>
{
    public async Task HandleAsync(InboundOrderCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        var productIds = notification.Items.Select(i => i.ProductId).ToList();
        var products = (await productRepository.GetByIdsAsync(productIds, cancellationToken)).Value
            .ToDictionary(p => p.Id);

        var integrationEvent = new InboundOrderCreatedIntegrationEvent(
            notification.InboundOrderId,
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
