namespace MessageContract.OutboundOrders;

public sealed record OutboundOrderConfirmedIntegrationEvent(
    Guid OutboundOrderId,
    Guid WarehouseId,
    Guid ConfirmedBy
) : IntegrationEvent, INotificationIntegrationEvent;
