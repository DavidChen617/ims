using Davish.Result;
using Domain.OutboundOrders;

namespace Application.Outbound;

public interface IOutboundOrderReader
{
    Task<Result<OutboundOrderDto>> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Result<OutboundOrderDto>> GetByIdAsync(Guid id, Guid warehouseId, CancellationToken ct);
    Task<Result<PagedResult<PendingOutboundOrderDto>>> ListPendingAsync(Guid warehouseId, int page, int size, CancellationToken ct);

    Task<Result<PagedResult<OutboundHistoryDto>>> ListHistoryAsync(
        Guid? warehouseId,
        string? orderNo,
        OutboundOrderStatus? status,
        DateTime? requestedFrom,
        DateTime? requestedTo,
        DateTime? completedFrom,
        DateTime? completedTo,
        string? productNo,
        string? productName,
        string? unit,
        string? requestedByName,
        string? confirmedByName,
        int page,
        int size,
        CancellationToken ct);

    Task<Result<IReadOnlyList<PendingOutboundQuantityDto>>> ListPendingQuantitiesAsync(
        Guid? warehouseId, Guid? productId, CancellationToken ct);
}
