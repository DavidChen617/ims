using MessageContract.OutboundOrders;
using Microsoft.Extensions.Logging;

namespace Application.Stocks.EventHandling;

public sealed class OutboundOrderRejectedIntegrationEventHandler(
    Domain.Stocks.IStockRepository repository,
    ILogger<OutboundOrderRejectedIntegrationEventHandler> logger
) : MessageContract.IIntegrationEventHandler<OutboundOrderRejectedIntegrationEvent>
{
    public async Task HandleAsync(OutboundOrderRejectedIntegrationEvent notification, CancellationToken cancellationToken)
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
                    "OutboundOrderRejectedIntegrationEvent: {EventId} received but no stock row exists for product {ProductId} in warehouse {WarehouseId}.",
                    notification.Id, item.ProductId, notification.WarehouseId);
                continue;
            }

            stock.ReleaseReservation(item.Quantity);
        }

        await repository.SaveRangeAsync(stocksByProductId.Values.ToList(), cancellationToken);
    }
}
