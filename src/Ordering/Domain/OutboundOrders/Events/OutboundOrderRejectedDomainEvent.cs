using SharedKernel;

namespace Domain.OutboundOrders.Events;

public sealed record OutboundOrderRejectedDomainEvent(
    Guid OutboundOrderId,
    Guid WarehouseId,
    Guid RejectedBy,
    string Reason,
    IReadOnlyList<(Guid ProductId, int Quantity)> Items) : IDomainEvent
{
    public Guid Id { get; } = Guid.CreateVersion7();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
