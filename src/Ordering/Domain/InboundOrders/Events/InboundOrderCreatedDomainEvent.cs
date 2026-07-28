using SharedKernel;

namespace Domain.InboundOrders.Events;

public sealed record InboundOrderCreatedDomainEvent(
    Guid InboundOrderId,
    Guid WarehouseId,
    IReadOnlyList<(Guid ProductId, int Quantity)> Items) : IDomainEvent
{
    public Guid Id { get; } = Guid.CreateVersion7();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
