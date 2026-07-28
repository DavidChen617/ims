using Davish.Result;

namespace Domain.OutboundOrders;

public interface IOutboundOrderRepository
{
    Task<Result> AddAsync(OutboundOrder order, CancellationToken ct);
    Task<Result<OutboundOrder>> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Result> SaveAsync(OutboundOrder order, CancellationToken ct);
}
