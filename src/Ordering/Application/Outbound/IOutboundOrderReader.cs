using Davish.Result;
using Domain.OutboundOrders;

namespace Application.Outbound;

public interface IOutboundOrderReader
{
    Task<Result<OutboundOrderDto>> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Result<OutboundOrderDto>> GetByIdAsync(Guid id, Guid warehouseId, CancellationToken ct);
    Task<Result<IReadOnlyList<PendingOutboundOrderDto>>> ListPendingAsync(Guid warehouseId, CancellationToken ct);

    Task<Result<IReadOnlyList<OutboundHistoryDto>>> ListHistoryAsync(
        Guid? warehouseId,
        OutboundOrderStatus? status,
        DateTime? completedFrom,
        DateTime? completedTo,
        string? productNo,
        string? productName,
        string? unit,
        Guid? requestedBy,
        Guid? confirmedBy,
        CancellationToken ct);

    Task<Result<IReadOnlyList<PendingOutboundQuantityDto>>> ListPendingQuantitiesAsync(
        Guid? warehouseId, Guid? productId, CancellationToken ct);
}
