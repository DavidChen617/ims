using Davish.Result;
using Domain.InboundOrders;

namespace Application.Inbound;

public interface IInboundOrderReader
{
    Task<Result<InboundOrderDto>> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Result<InboundOrderDto>> GetByIdAsync(Guid id, Guid warehouseId, CancellationToken ct);
    Task<Result<PagedResult<PendingInboundOrderDto>>> ListPendingAsync(Guid warehouseId, int page, int size, CancellationToken ct);

    Task<Result<InboundHistoryResultDto>> ListHistoryAsync(
        Guid? warehouseId,
        string? orderNo,
        string? productNo,
        string? productName,
        Guid? requestedBy,
        Guid? confirmedBy,
        InboundOrderStatus? status,
        DateTime? requestedFrom,
        DateTime? requestedTo,
        int? quantityMin,
        int? quantityMax,
        decimal? unitPriceMin,
        decimal? unitPriceMax,
        decimal? amountMin,
        decimal? amountMax,
        int page,
        int size,
        CancellationToken ct);

    Task<Result<IReadOnlyList<InboundOrderHistoryDto>>> ListOrderHistoryAsync(
        Guid warehouseId,
        InboundOrderStatus? status,
        string? productNo,
        string? productName,
        Guid? requestedBy,
        Guid? confirmedBy,
        DateTime? requestedFrom,
        DateTime? requestedTo,
        DateTime? completedFrom,
        DateTime? completedTo,
        decimal? amountMin,
        decimal? amountMax,
        CancellationToken ct);
}
