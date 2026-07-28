using SharedKernel;

namespace Domain.InboundOrders.Events;

public sealed record InboundOrderConfirmedDomainEvent(
    Guid InboundOrderId,
    Guid WarehouseId,
    Guid ConfirmedBy) : IDomainEvent
{
    public Guid Id { get; } = Guid.CreateVersion7();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
