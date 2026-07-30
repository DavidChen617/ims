using ISM_BACKEND.Base;
using ISM_BACKEND.Models;
using ISM_BACKEND.StateMachines;

namespace ISM_BACKEND.Services;

public class InboundOrderService
{
    private readonly DapperRepository _db;

    public InboundOrderService(DapperRepository db) => _db = db;

    // 確認入庫單才加庫存,建立時不動庫存。
    // (原本建立就先樂觀加庫存、拒絕才扣回去,兩者之間有時間窗:出庫單可能搶先訂走這批
    //  還沒確認的庫存,拒絕要扣回時可能不夠扣變成負數。改成確認才加,race condition 直接消失。)
    public async Task<long> CreateInboundOrderAsync(long warehouseId, long requestedBy, string requestedByName,
        string orderNo, List<CreateOrderItemRequest> items)
    {
        if (items.Count == 0)
            throw new ArgumentException("入庫單至少要有一項商品");
        if (items.Any(i => i.quantity <= 0))
            throw new ArgumentException("商品數量必須大於 0");

        var dupCount = await _db.QueryFirstOrDefaultAsync<int>(IsmQueries.CountInboundOrderByOrderNo, new { OrderNo = orderNo });
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
            var orderId = await _db.ExecuteInsertWithIdentityAsync(IsmQueries.InsertInboundOrder, new
            {
                OrderNo = orderNo,
                WarehouseId = warehouseId,
                Status = (int)OrderStatus.Pending,
                RequestedBy = requestedBy,
                RequestedByName = requestedByName
            });

            foreach (var item in items)
            {
                var product = products[item.productId];
                var unitPrice = item.unitPrice ?? product.Price;

                await _db.ExecuteAsync(IsmQueries.InsertInboundOrderItem, new
                {
                    InboundOrderId = orderId,
                    ProductId = item.productId,
                    Quantity = item.quantity,
                    UnitPrice = unitPrice
                });
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

    // 加庫存的動作搬到這裡:確認才是庫存真正入帳的時間點
    public async Task<bool> ConfirmInboundOrderAsync(long orderId, long confirmedBy, string confirmedByName)
    {
        var order = await _db.QueryFirstOrDefaultAsync<OrderRow>(IsmQueries.FindInboundOrderById, new { InboundOrderId = orderId });
        if (order == null || order.Status != (int)OrderStatus.Pending)
            return false;

        var items = (await _db.QueryAsync<ItemRow>(IsmQueries.ListInboundOrderItemsByOrderId, new { InboundOrderId = orderId })).ToList();

        _db.BeginTransaction();
        try
        {
            var affected = await _db.ExecuteAsync(IsmQueries.UpdateInboundOrderConfirm, new
            {
                InboundOrderId = orderId,
                Status = (int)OrderStatus.Confirmed,
                ConfirmedBy = confirmedBy,
                ConfirmedByName = confirmedByName,
                PendingStatus = (int)OrderStatus.Pending
            });

            if (affected == 0)
            {
                _db.Rollback();
                return false;
            }

            foreach (var item in items)
                await IncreaseStockAsync(item.ProductId, order.WarehouseId, item.Quantity);

            _db.Commit();
            return true;
        }
        catch
        {
            _db.Rollback();
            throw;
        }
    }

    // 拒絕入庫單不用動庫存:建立時本來就沒加,沒有東西要沖銷
    public async Task<bool> RejectInboundOrderAsync(long orderId, long rejectedBy, string rejectedByName, string reason)
    {
        _db.BeginTransaction();
        try
        {
            var affected = await _db.ExecuteAsync(IsmQueries.UpdateInboundOrderReject, new
            {
                InboundOrderId = orderId,
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

            _db.Commit();
            return true;
        }
        catch
        {
            _db.Rollback();
            throw;
        }
    }

    public async Task<OrderDetail?> GetInboundOrderAsync(long orderId, long? scopedWarehouseId)
    {
        var order = await _db.QueryFirstOrDefaultAsync<OrderRow>(IsmQueries.FindInboundOrderById, new { InboundOrderId = orderId });
        if (order == null)
            return null;
        if (scopedWarehouseId != null && order.WarehouseId != scopedWarehouseId)
            return null;

        var items = (await _db.QueryAsync<ItemRow>(IsmQueries.ListInboundOrderItemsByOrderId, new { InboundOrderId = orderId })).ToList();
        return MapDetail(order, items);
    }

    public async Task<PagedResult<OrderListItem>> ListInboundOrdersAsync(long? warehouseId, string? status, string? orderNo, int page, int pageSize)
    {
        var statusCode = string.IsNullOrEmpty(status) ? (int?)null : (int)OrderStateMachine.FromApiString(status);
        var param = new { WarehouseId = warehouseId, Status = statusCode, OrderNo = orderNo, Offset = (page - 1) * pageSize, PageSize = pageSize };

        var rows = (await _db.QueryAsync<OrderListRow>(IsmQueries.ListInboundOrders, param)).ToList();
        var total = await _db.QueryFirstOrDefaultAsync<int>(IsmQueries.CountInboundOrders, param);

        return new PagedResult<OrderListItem>
        {
            items = rows.Select(MapListItem).ToList(),
            meta = new PaginationMeta { page = page, pageSize = pageSize, total = total }
        };
    }

    private async Task IncreaseStockAsync(long productId, long warehouseId, int quantity)
    {
        var stock = await _db.QueryFirstOrDefaultAsync<StockRow>(IsmQueries.FindStockByProductWarehouse, new { ProductId = productId, WarehouseId = warehouseId });
        if (stock == null)
        {
            var stockId = await _db.ExecuteInsertWithIdentityAsync(IsmQueries.InsertStock, new { ProductId = productId, WarehouseId = warehouseId });
            await _db.ExecuteAsync(IsmQueries.UpdateStockQuantity, new { StockId = stockId, Quantity = quantity, CumulativeShipped = 0 });
            return;
        }

        await _db.ExecuteAsync(IsmQueries.UpdateStockQuantity, new
        {
            StockId = stock.StockId,
            Quantity = stock.Quantity + quantity,
            CumulativeShipped = stock.CumulativeShipped
        });
    }

    private static OrderDetail MapDetail(OrderRow order, List<ItemRow> items) => new()
    {
        orderId = order.InboundOrderId,
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
            quantity = i.Quantity,
            unitPrice = i.UnitPrice
        }).ToList()
    };

    private static OrderListItem MapListItem(OrderListRow row) => new()
    {
        orderId = row.InboundOrderId,
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
        public long InboundOrderId { get; set; }
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
        public decimal UnitPrice { get; set; }
    }

    private sealed class OrderListRow
    {
        public long InboundOrderId { get; set; }
        public string OrderNo { get; set; } = "";
        public long WarehouseId { get; set; }
        public int Status { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? ConfirmedAt { get; set; }
    }
}
