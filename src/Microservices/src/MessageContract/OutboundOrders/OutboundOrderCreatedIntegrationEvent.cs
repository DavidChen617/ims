namespace MessageContract.OutboundOrders;

public sealed record OutboundOrderCreatedIntegrationEvent(
    Guid OutboundOrderId,
    Guid WarehouseId,
    string? WarehouseName,
    IReadOnlyList<EnrichedOrderItem> Items
) : IntegrationEvent, INotificationIntegrationEvent;
