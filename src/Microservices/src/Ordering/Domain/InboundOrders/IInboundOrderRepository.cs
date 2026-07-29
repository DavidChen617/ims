using Davish.Result;

namespace Domain.InboundOrders;

public interface IInboundOrderRepository
{
    Task<Result> AddAsync(InboundOrder order, CancellationToken ct);
    Task<Result<InboundOrder>> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Result> SaveAsync(InboundOrder order, CancellationToken ct);
}
