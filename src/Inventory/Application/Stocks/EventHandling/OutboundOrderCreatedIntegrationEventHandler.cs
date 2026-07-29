using Domain.Stocks;
using MessageContract.OutboundOrders;
using Microsoft.Extensions.Logging;
using SharedKernel;

using Application.Abstracts;
namespace Application.Stocks.EventHandling;

public sealed class OutboundOrderCreatedIntegrationEventHandler(
    IStockRepository repository,
    IUnitOfWork unitOfWork,
    IIntegrationEventWriter writer,
    ILogger<OutboundOrderCreatedIntegrationEventHandler> logger
) : MessageContract.IIntegrationEventHandler<OutboundOrderCreatedIntegrationEvent>
{
    public async Task HandleAsync(OutboundOrderCreatedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var productIds = notification.Items.Select(i => i.ProductId).Distinct().ToList();
        var stocks = await repository.GetOrCreateManyAsync(productIds, notification.WarehouseId, cancellationToken);
        var stocksByProductId = stocks.ToDictionary(s => s.ProductId);

        var insufficientProductIds = new List<Guid>();

        foreach (var item in notification.Items)
        {
            var stock = stocksByProductId[item.ProductId];
            var reserveResult = stock.TryReserve(item.Quantity);

            if (!reserveResult.IsSuccess)
            {
                insufficientProductIds.Add(item.ProductId);
                continue;
            }

            stock.SetDisplayInfo(item.ProductNo, item.ProductName, item.Unit, notification.WarehouseName);
        }

        if (insufficientProductIds.Count > 0)
        {
            await unitOfWork.RollbackAsync(cancellationToken);

            logger.LogWarning(
                "OutboundOrderCreatedIntegrationEvent: {EventId} for order {OutboundOrderId} could not reserve products {ProductIds}.",
                notification.Id, notification.OutboundOrderId, insufficientProductIds);

            // 全新的 transaction:上面那次預留嘗試已經被 rollback 掉了,不應該有任何東西延續到「記錄失敗通知(以及 inbox 標記)」這一步裡。
            await unitOfWork.BeginAsync(cancellationToken);

            await writer.WriteAsync(
                new OutboundInventoryReservationFailedIntegrationEvent(
                    notification.OutboundOrderId, notification.WarehouseId, insufficientProductIds),
                cancellationToken);

            return;
        }

        await repository.SaveRangeAsync(stocks, cancellationToken);

        await writer.WriteAsync(
            new OutboundInventoryReservedIntegrationEvent(notification.OutboundOrderId, notification.WarehouseId),
            cancellationToken);
    }
}
