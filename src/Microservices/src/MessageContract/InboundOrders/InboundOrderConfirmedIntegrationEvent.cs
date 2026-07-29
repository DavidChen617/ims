namespace MessageContract.InboundOrders;

public sealed record InboundOrderConfirmedIntegrationEvent(
    Guid InboundOrderId,
    Guid WarehouseId,
    Guid ConfirmedBy
) : IntegrationEvent, INotificationIntegrationEvent;
