namespace MessageContract.InboundOrders;

public sealed record InboundOrderRejectedIntegrationEvent(
    Guid InboundOrderId,
    Guid WarehouseId,
    Guid RejectedBy,
    string Reason,
    IReadOnlyList<OrderItem> Items
) : IntegrationEvent, INotificationIntegrationEvent;
