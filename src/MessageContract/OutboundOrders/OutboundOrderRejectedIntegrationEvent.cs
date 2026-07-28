namespace MessageContract.OutboundOrders;

public sealed record OutboundOrderRejectedIntegrationEvent(
    Guid OutboundOrderId,
    Guid WarehouseId,
    Guid RejectedBy,
    string Reason,
    IReadOnlyList<OrderItem> Items
) : IntegrationEvent, INotificationIntegrationEvent;
