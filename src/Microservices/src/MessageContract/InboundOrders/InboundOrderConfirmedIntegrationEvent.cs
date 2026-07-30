namespace MessageContract.InboundOrders;

public sealed record InboundOrderConfirmedIntegrationEvent(
    Guid InboundOrderId,
    Guid WarehouseId,
    string? WarehouseName,
    Guid ConfirmedBy,
    IReadOnlyList<EnrichedOrderItem> Items
) : IntegrationEvent, INotificationIntegrationEvent;
