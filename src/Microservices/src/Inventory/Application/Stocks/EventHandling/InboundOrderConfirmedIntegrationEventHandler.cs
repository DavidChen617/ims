using Domain.Stocks;
using MessageContract.InboundOrders;

namespace Application.Stocks.EventHandling;

public sealed class InboundOrderConfirmedIntegrationEventHandler(
    IStockRepository repository
) : MessageContract.IIntegrationEventHandler<InboundOrderConfirmedIntegrationEvent>
{
    public async Task HandleAsync(InboundOrderConfirmedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var productIds = notification.Items.Select(i => i.ProductId).Distinct().ToList();
        var stocks = await repository.GetOrCreateManyAsync(productIds, notification.WarehouseId, cancellationToken);
        var stocksByProductId = stocks.ToDictionary(s => s.ProductId);

        foreach (var item in notification.Items)
        {
            var stock = stocksByProductId[item.ProductId];
            stock.Increase(item.Quantity);
            stock.SetDisplayInfo(item.ProductNo, item.ProductName, item.Unit, notification.WarehouseName);
        }

        await repository.SaveRangeAsync(stocks, cancellationToken);
    }
}
