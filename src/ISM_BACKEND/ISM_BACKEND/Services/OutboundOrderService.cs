using ISM_BACKEND.Base;
using ISM_BACKEND.Models;
using ISM_BACKEND.StateMachines;

namespace ISM_BACKEND.Services;

public class OutboundOrderService
{
    private const string InsufficientStockReason = "庫存不足,系統自動拒絕";

    private readonly DapperRepository _db;

    public OutboundOrderService(DapperRepository db) => _db = db;

    public async Task<long> CreateOutboundOrderAsync(long warehouseId, long requestedBy, string requestedByName,
        string orderNo, List<CreateOrderItemRequest> items)
    {
        if (items.Count == 0)
            throw new ArgumentException("出庫單至少要有一項商品");
        if (items.Any(i => i.quantity <= 0))
            throw new ArgumentException("商品數量必須大於 0");

        var dupCount = await _db.QueryFirstOrDefaultAsync<int>(IsmQueries.CountOutboundOrderByOrderNo, new { OrderNo = orderNo });
        if (dupCount > 0)
            throw new ArgumentException($"單號 {orderNo} 已存在");

        var productIds = items.Select(i => i.productId).ToArray();
        var products = (await _db.QueryAsync<ProductLookupRow>(IsmQueries.FindProductsByIds, new { ProductIds = productIds }))
            .ToDictionary(p => p.ProductId);

        var missing = productIds.Where(id => !products.ContainsKey(id)).ToList();
        if (missing.Count > 0)
            throw new ArgumentException($"商品不存在: {string.Join(",", missing)}");

        _db.BeginTransaction();
        try
        {
            var stocks = new Dictionary<long, StockRow?>();
            foreach (var item in items)
                stocks[item.productId] = await _db.QueryFirstOrDefaultAsync<StockRow>(
                    IsmQueries.FindStockByProductWarehouse, new { ProductId = item.productId, WarehouseId = warehouseId });

            var insufficientProductIds = items
                .Where(i => (stocks[i.productId]?.Quantity ?? 0) < i.quantity)
                .Select(i => i.productId)
                .ToList();

            var isReserved = insufficientProductIds.Count == 0;

            var orderId = await _db.ExecuteInsertWithIdentityAsync(IsmQueries.InsertOutboundOrder, new
            {
                OrderNo = orderNo,
                WarehouseId = warehouseId,
                Status = isReserved ? (int)OrderStatus.Pending : (int)OrderStatus.Rejected,
                RejectReason = isReserved ? null : InsufficientStockReason,
                RequestedBy = requestedBy,
                RequestedByName = requestedByName
            });

            foreach (var item in items)
            {
                await _db.ExecuteAsync(IsmQueries.InsertOutboundOrderItem, new
                {
                    OutboundOrderId = orderId,
                    ProductId = item.productId,
                    Quantity = item.quantity
                });

                if (isReserved)
                    await ReserveStockAsync(stocks[item.productId], item.productId, warehouseId, item.quantity);
            }

            _db.Commit();
            return orderId;
        }
        catch
        {
            _db.Rollback();
            throw;
        }
    }

    public async Task<bool> ConfirmOutboundOrderAsync(long orderId, long confirmedBy, string confirmedByName)
    {
        // 已在建單時扣過庫存，確認只轉態不動庫存
        var affected = await _db.ExecuteAsync(IsmQueries.UpdateOutboundOrderConfirm, new
        {
            OutboundOrderId = orderId,
            Status = (int)OrderStatus.Confirmed,
            ConfirmedBy = confirmedBy,
            ConfirmedByName = confirmedByName,
            PendingStatus = (int)OrderStatus.Pending
        });
        return affected > 0;
    }

    // WarehouseAdmin 主動拒絕(僅能對 Pending=已預留成功的單操作)要釋放預留的庫存
    public async Task<bool> RejectOutboundOrderAsync(long orderId, long rejectedBy, string rejectedByName, string reason)
    {
        var order = await _db.QueryFirstOrDefaultAsync<OrderRow>(IsmQueries.FindOutboundOrderById, new { OutboundOrderId = orderId });
        if (order == null || order.Status != (int)OrderStatus.Pending)
            return false;

        var items = (await _db.QueryAsync<ItemRow>(IsmQueries.ListOutboundOrderItemsByOrderId, new { OutboundOrderId = orderId })).ToList();

        _db.BeginTransaction();
        try
        {
            var affected = await _db.ExecuteAsync(IsmQueries.UpdateOutboundOrderReject, new
            {
                OutboundOrderId = orderId,
                Status = (int)OrderStatus.Rejected,
                RejectReason = reason,
                ConfirmedBy = rejectedBy,
                ConfirmedByName = rejectedByName,
                PendingStatus = (int)OrderStatus.Pending
            });

            if (affected == 0)
            {
                _db.Rollback();
                return false;
            }

            foreach (var item in items)
                await ReleaseReservationAsync(item.ProductId, order.WarehouseId, item.Quantity);

            _db.Commit();
            return true;
        }
        catch
        {
            _db.Rollback();
            throw;
        }
    }

    public async Task<OrderDetail?> GetOutboundOrderAsync(long orderId, long? scopedWarehouseId)
    {
        var order = await _db.QueryFirstOrDefaultAsync<OrderRow>(IsmQueries.FindOutboundOrderById, new { OutboundOrderId = orderId });
        if (order == null)
            return null;
        if (scopedWarehouseId != null && order.WarehouseId != scopedWarehouseId)
            return null;

        var items = (await _db.QueryAsync<ItemRow>(IsmQueries.ListOutboundOrderItemsByOrderId, new { OutboundOrderId = orderId })).ToList();
        return MapDetail(order, items);
    }

    public async Task<PagedResult<OrderListItem>> ListOutboundOrdersAsync(long? warehouseId, string? status, string? orderNo, int page, int pageSize)
    {
        var statusCode = string.IsNullOrEmpty(status) ? (int?)null : (int)OrderStateMachine.FromApiString(status);
        var param = new { WarehouseId = warehouseId, Status = statusCode, OrderNo = orderNo, Offset = (page - 1) * pageSize, PageSize = pageSize };

        var rows = (await _db.QueryAsync<OrderListRow>(IsmQueries.ListOutboundOrders, param)).ToList();
        var total = await _db.QueryFirstOrDefaultAsync<int>(IsmQueries.CountOutboundOrders, param);

        return new PagedResult<OrderListItem>
        {
            items = rows.Select(MapListItem).ToList(),
            meta = new PaginationMeta { page = page, pageSize = pageSize, total = total }
        };
    }

    private async Task ReserveStockAsync(StockRow? existing, long productId, long warehouseId, int quantity)
    {
        if (existing == null)
        {
            // 理論上不會發生(不足量早被擋在 insufficientProductIds),保底寫法比照 FWMP 的防呆慣例
            var stockId = await _db.ExecuteInsertWithIdentityAsync(IsmQueries.InsertStock, new { ProductId = productId, WarehouseId = warehouseId });
            await _db.ExecuteAsync(IsmQueries.UpdateStockQuantity, new { StockId = stockId, Quantity = 0 - quantity, CumulativeShipped = quantity });
            return;
        }

        await _db.ExecuteAsync(IsmQueries.UpdateStockQuantity, new
        {
            StockId = existing.StockId,
            Quantity = existing.Quantity - quantity,
            CumulativeShipped = existing.CumulativeShipped + quantity
        });
    }

    private async Task ReleaseReservationAsync(long productId, long warehouseId, int quantity)
    {
        var stock = await _db.QueryFirstOrDefaultAsync<StockRow>(IsmQueries.FindStockByProductWarehouse, new { ProductId = productId, WarehouseId = warehouseId });
        if (stock == null)
            return;

        await _db.ExecuteAsync(IsmQueries.UpdateStockQuantity, new
        {
            StockId = stock.StockId,
            Quantity = stock.Quantity + quantity,
            CumulativeShipped = stock.CumulativeShipped - quantity
        });
    }

    private static OrderDetail MapDetail(OrderRow order, List<ItemRow> items) => new()
    {
        orderId = order.OutboundOrderId,
        orderNo = order.OrderNo,
        warehouseId = order.WarehouseId,
        status = OrderStateMachine.ToApiString((OrderStatus)order.Status),
        rejectReason = order.RejectReason,
        requestedBy = order.RequestedBy,
        requestedByName = order.RequestedByName,
        requestedAt = order.RequestedAt,
        confirmedBy = order.ConfirmedBy,
        confirmedByName = order.ConfirmedByName,
        confirmedAt = order.ConfirmedAt,
        items = items.Select(i => new OrderItemDto
        {
            productId = i.ProductId,
            productNo = i.ProductNo,
            productName = i.ProductName,
            unit = i.Unit,
            quantity = i.Quantity
        }).ToList()
    };

    private static OrderListItem MapListItem(OrderListRow row) => new()
    {
        orderId = row.OutboundOrderId,
        orderNo = row.OrderNo,
        warehouseId = row.WarehouseId,
        status = OrderStateMachine.ToApiString((OrderStatus)row.Status),
        requestedAt = row.RequestedAt,
        confirmedAt = row.ConfirmedAt
    };

    private sealed class ProductLookupRow
    {
        public long ProductId { get; set; }
        public string ProductNo { get; set; } = "";
        public string Name { get; set; } = "";
        public string Unit { get; set; } = "";
        public decimal Price { get; set; }
    }

    private sealed class StockRow
    {
        public long StockId { get; set; }
        public long ProductId { get; set; }
        public long WarehouseId { get; set; }
        public int Quantity { get; set; }
        public int CumulativeShipped { get; set; }
    }

    private sealed class OrderRow
    {
        public long OutboundOrderId { get; set; }
        public string OrderNo { get; set; } = "";
        public long WarehouseId { get; set; }
        public int Status { get; set; }
        public string? RejectReason { get; set; }
        public long RequestedBy { get; set; }
        public string RequestedByName { get; set; } = "";
        public DateTime RequestedAt { get; set; }
        public long? ConfirmedBy { get; set; }
        public string? ConfirmedByName { get; set; }
        public DateTime? ConfirmedAt { get; set; }
    }

    private sealed class ItemRow
    {
        public long ProductId { get; set; }
        public string ProductNo { get; set; } = "";
        public string ProductName { get; set; } = "";
        public string Unit { get; set; } = "";
        public int Quantity { get; set; }
    }

    private sealed class OrderListRow
    {
        public long OutboundOrderId { get; set; }
        public string OrderNo { get; set; } = "";
        public long WarehouseId { get; set; }
        public int Status { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? ConfirmedAt { get; set; }
    }
}
