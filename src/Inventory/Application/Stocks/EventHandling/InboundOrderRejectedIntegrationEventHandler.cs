using MessageContract.InboundOrders;
using Microsoft.Extensions.Logging;

namespace Application.Stocks.EventHandling;

public sealed class InboundOrderRejectedIntegrationEventHandler(
    Domain.Stocks.IStockRepository repository,
    ILogger<InboundOrderRejectedIntegrationEventHandler> logger
) : MessageContract.IIntegrationEventHandler<InboundOrderRejectedIntegrationEvent>
{
    public async Task HandleAsync(InboundOrderRejectedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var productIds = notification.Items.Select(i => i.ProductId).Distinct().ToList();
        var existingResult = await repository.GetByProductsAndWarehouseAsync(
            productIds, notification.WarehouseId, cancellationToken);
        var stocksByProductId = existingResult.Value.ToDictionary(s => s.ProductId);

        foreach (var item in notification.Items)
        {
            if (!stocksByProductId.TryGetValue(item.ProductId, out var stock))
            {
                logger.LogError(
                    "InboundOrderRejectedIntegrationEvent: {EventId} received but no stock row exists for product {ProductId} in warehouse {WarehouseId}.",
                    notification.Id, item.ProductId, notification.WarehouseId);
                continue;
            }

            stock.Decrease(item.Quantity);
        }

        await repository.SaveRangeAsync(stocksByProductId.Values.ToList(), cancellationToken);
    }
}
