namespace MessageContract.OutboundOrders;

public sealed record OutboundInventoryReservationFailedIntegrationEvent(
    Guid OutboundOrderId,
    Guid WarehouseId,
    IReadOnlyList<Guid> InsufficientProductIds
) : IntegrationEvent, INotificationIntegrationEvent;
