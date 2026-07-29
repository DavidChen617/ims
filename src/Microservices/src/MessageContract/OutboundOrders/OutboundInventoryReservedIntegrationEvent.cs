namespace MessageContract.OutboundOrders;

public sealed record OutboundInventoryReservedIntegrationEvent(
    Guid OutboundOrderId,
    Guid WarehouseId
) : IntegrationEvent, INotificationIntegrationEvent;
