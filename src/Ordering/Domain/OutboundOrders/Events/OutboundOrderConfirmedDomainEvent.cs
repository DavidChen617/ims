using SharedKernel;

namespace Domain.OutboundOrders.Events;

public sealed record OutboundOrderConfirmedDomainEvent(
    Guid OutboundOrderId,
    Guid WarehouseId,
    Guid ConfirmedBy) : IDomainEvent
{
    public Guid Id { get; } = Guid.CreateVersion7();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
