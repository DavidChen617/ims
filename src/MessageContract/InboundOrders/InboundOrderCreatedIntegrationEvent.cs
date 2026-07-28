namespace MessageContract.InboundOrders;

public sealed record InboundOrderCreatedIntegrationEvent(
    Guid InboundOrderId,
    Guid WarehouseId,
    string? WarehouseName,
    IReadOnlyList<EnrichedOrderItem> Items
) : IntegrationEvent, INotificationIntegrationEvent;
