using SharedKernel;

namespace Domain.InboundOrders.Events;

public sealed record InboundOrderConfirmedDomainEvent(
    Guid InboundOrderId,
    Guid WarehouseId,
    Guid ConfirmedBy,
    IReadOnlyList<(Guid ProductId, int Quantity)> Items) : IDomainEvent
{
    public Guid Id { get; } = Guid.CreateVersion7();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
