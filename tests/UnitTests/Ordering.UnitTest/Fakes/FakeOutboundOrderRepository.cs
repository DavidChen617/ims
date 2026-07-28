using Davish.Result;
using Domain.OutboundOrders;

namespace Ordering.UnitTest.Fakes;

public sealed class FakeOutboundOrderRepository : IOutboundOrderRepository
{
    public OutboundOrder? OrderToReturn { get; set; }
    public OutboundOrder? Saved { get; private set; }

    public Task<Result> AddAsync(OutboundOrder order, CancellationToken ct)
        => throw new NotSupportedException("Not needed by the handlers under test.");

    public Task<Result<OutboundOrder>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        Result<OutboundOrder> result = OrderToReturn is null
            ? new Error("OutboundOrder.NotFound", "Outbound order not found", ErrorType.NotFound)
            : OrderToReturn;

        return Task.FromResult(result);
    }

    public Task<Result> SaveAsync(OutboundOrder order, CancellationToken ct)
    {
        Saved = order;
        return Task.FromResult(Result.Success());
    }
}
