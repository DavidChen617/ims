using MessageContract.InboundOrders;
using Microsoft.Extensions.Logging;

namespace Application.Stocks.EventHandling;

public sealed class InboundOrderRejectedIntegrationEventHandler(
    Domain.Stocks.IStockRepository repository,
    ILogger<InboundOrderRejectedIntegrationEventHandler> logger
) : MessageContract.IIntegrationEventHandler<InboundOrderRejectedIntegrationEvent>
{
    // 去重(BeginIfNotProcessedAsync)現在在 IntegrationEventConsumer 裡就做完了,
    // 早於這個 handler 被派送到之前 —— 我們進來的時候 transaction 已經是開著的了。
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
