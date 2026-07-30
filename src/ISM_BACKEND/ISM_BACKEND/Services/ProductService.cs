using ISM_BACKEND.Base;
using ISM_BACKEND.Models;

namespace ISM_BACKEND.Services;

public class ProductService
{
    private readonly DapperRepository _db;

    public ProductService(DapperRepository db) => _db = db;

    public async Task CreateProductUnitAsync(string name)
    {
        var count = await _db.QueryFirstOrDefaultAsync<int>(IsmQueries.CountProductUnitByName, new { Name = name });
        if (count > 0)
            throw new ArgumentException($"商品單位 {name} 已存在");

        await _db.ExecuteAsync(IsmQueries.InsertProductUnit, new { Name = name });
    }

    public async Task<bool> DeleteProductUnitAsync(string name)
    {
        var usedCount = await _db.QueryFirstOrDefaultAsync<int>(IsmQueries.CountProductByUnit, new { Name = name });
        if (usedCount > 0)
            throw new ArgumentException($"商品單位 {name} 仍被商品使用,無法刪除");

        var affected = await _db.ExecuteAsync(IsmQueries.DeleteProductUnit, new { Name = name });
        return affected > 0;
    }

    public async Task<List<ProductUnitItem>> ListProductUnitsAsync()
    {
        var rows = await _db.QueryAsync<string>(IsmQueries.ListProductUnits);
        return rows.Select(n => new ProductUnitItem { name = n }).ToList();
    }

    public async Task<long> CreateProductAsync(string productNo, string name, string unit, decimal price)
    {
        var noCount = await _db.QueryFirstOrDefaultAsync<int>(IsmQueries.CountProductByNo, new { ProductNo = productNo });
        if (noCount > 0)
            throw new ArgumentException($"商品編號 {productNo} 已存在");

        var unitExists = await _db.QueryFirstOrDefaultAsync<string>(IsmQueries.FindProductUnitByName, new { Name = unit });
        if (unitExists == null)
            throw new ArgumentException($"商品單位 {unit} 不存在");

        _db.BeginTransaction();
        try
        {
            var id = await _db.ExecuteInsertWithIdentityAsync(IsmQueries.InsertProduct, new { ProductNo = productNo, Name = name, Unit = unit, Price = price });
            _db.Commit();
            return id;
        }
        catch
        {
            _db.Rollback();
            throw;
        }
    }

    public async Task<PagedResult<ProductItem>> ListProductsAsync(string? productNo, string? name, string? unit, int page, int pageSize)
    {
        var param = new { ProductNo = productNo, Name = name, Unit = unit, Offset = (page - 1) * pageSize, PageSize = pageSize };
        var rows = (await _db.QueryAsync<ProductRow>(IsmQueries.ListProducts, param)).ToList();
        var total = await _db.QueryFirstOrDefaultAsync<int>(IsmQueries.CountProducts, param);

        return new PagedResult<ProductItem>
        {
            items = rows.Select(Map).ToList(),
            meta = new PaginationMeta { page = page, pageSize = pageSize, total = total }
        };
    }

    public async Task<ProductItem?> GetProductAsync(long id)
    {
        var row = await _db.QueryFirstOrDefaultAsync<ProductRow>(IsmQueries.FindProductById, new { ProductId = id });
        return row == null ? null : Map(row);
    }

    private static ProductItem Map(ProductRow row) => new()
    {
        productId = row.ProductId,
        productNo = row.ProductNo,
        name = row.Name,
        unit = row.Unit,
        price = row.Price
    };

    private sealed class ProductRow
    {
        public long ProductId { get; set; }
        public string ProductNo { get; set; } = "";
        public string Name { get; set; } = "";
        public string Unit { get; set; } = "";
        public decimal Price { get; set; }
    }
}
