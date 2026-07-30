using ISM_BACKEND.Base;
using ISM_BACKEND.Models;

namespace ISM_BACKEND.Services;

public class StockService
{
    private readonly DapperRepository _db;

    public StockService(DapperRepository db) => _db = db;

    public async Task<PagedResult<StockItem>> ListStocksAsync(long? warehouseId, long? productId, int page, int pageSize)
    {
        var param = new { WarehouseId = warehouseId, ProductId = productId, Offset = (page - 1) * pageSize, PageSize = pageSize };
        var rows = (await _db.QueryAsync<StockRow>(IsmQueries.ListStocks, param)).ToList();
        var total = await _db.QueryFirstOrDefaultAsync<int>(IsmQueries.CountStocks, param);

        return new PagedResult<StockItem>
        {
            items = rows.Select(Map).ToList(),
            meta = new PaginationMeta { page = page, pageSize = pageSize, total = total }
        };
    }

    private static StockItem Map(StockRow row) => new()
    {
        productId = row.ProductId,
        productNo = row.ProductNo,
        productName = row.ProductName,
        unit = row.Unit,
        warehouseId = row.WarehouseId,
        warehouseName = row.WarehouseName,
        quantity = row.Quantity,
        cumulativeShipped = row.CumulativeShipped
    };

    private sealed class StockRow
    {
        public long ProductId { get; set; }
        public string ProductNo { get; set; } = "";
        public string ProductName { get; set; } = "";
        public string Unit { get; set; } = "";
        public long WarehouseId { get; set; }
        public string WarehouseName { get; set; } = "";
        public int Quantity { get; set; }
        public int CumulativeShipped { get; set; }
    }
}
