using Dapper;
using Davish.Result;
using Domain.Stocks;

namespace Infrastructure.Persistence.Repositories;

public sealed class StockRepository(IInventoryUnitOfWork unitOfWork) : IStockRepository
{
    public async Task<Result<Stock>> GetByProductAndWarehouseAsync(Guid productId, Guid warehouseId, CancellationToken ct)
    {
        var cmd = new CommandDefinition(
            """
            select id, product_id, warehouse_id, quantity, cumulative_shipped,
                   product_no, product_name, unit, warehouse_name
            from stocks
            where product_id = @ProductId and warehouse_id = @WarehouseId
            """,
            new { ProductId = productId, WarehouseId = warehouseId },
            cancellationToken: ct,
            transaction: unitOfWork.Transaction
        );

        var stock = await unitOfWork.Connection.QuerySingleOrDefaultAsync<Stock>(cmd);

        return stock is null
            ? new Error("Stock.NotFound", "Stock not found", ErrorType.NotFound)
            : stock;
    }

    public async Task<Result> AddAsync(Stock stock, CancellationToken ct)
    {
        var cmd = new CommandDefinition(
            """
            insert into stocks(id, product_id, warehouse_id, quantity, cumulative_shipped,
                               product_no, product_name, unit, warehouse_name)
            values(@Id, @ProductId, @WarehouseId, @Quantity, @CumulativeShipped,
                   @ProductNo, @ProductName, @Unit, @WarehouseName)
            """,
            stock,
            cancellationToken: ct,
            transaction: unitOfWork.Transaction
        );

        await unitOfWork.Connection.ExecuteAsync(cmd);
        stock.ClearPendingChanges();

        return Result.Success();
    }

    public async Task<Result> SaveAsync(Stock stock, CancellationToken ct)
    {
        // 用相對值(quantity + @QuantityDelta)做原子更新,並在同一個 WHERE 內檢查結果不會變負數,
        // 不使用算好值後直接覆蓋的作法,避免併發寫入互相蓋掉對方的異動(lost update)。
        var cmd = new CommandDefinition(
            """
            update stocks
            set quantity = quantity + @QuantityDelta,
                cumulative_shipped = cumulative_shipped + @CumulativeShippedDelta,
                product_no = @ProductNo, product_name = @ProductName,
                unit = @Unit, warehouse_name = @WarehouseName
            where id = @Id
              and quantity + @QuantityDelta >= 0
              and cumulative_shipped + @CumulativeShippedDelta >= 0
            """,
            stock,
            cancellationToken: ct,
            transaction: unitOfWork.Transaction
        );

        var affected = await unitOfWork.Connection.ExecuteAsync(cmd);

        if (affected == 0)
            return new Error("Stock.Save", "Stock not found or the update would make quantity/cumulative shipped negative", ErrorType.Conflict);

        stock.ClearPendingChanges();
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<Stock>>> GetByProductsAndWarehouseAsync(
        IReadOnlyList<Guid> productIds, Guid warehouseId, CancellationToken ct)
    {
        var cmd = new CommandDefinition(
            """
            select id, product_id, warehouse_id, quantity, cumulative_shipped,
                   product_no, product_name, unit, warehouse_name
            from stocks
            where warehouse_id = @WarehouseId and product_id = any(@ProductIds)
            """,
            new { WarehouseId = warehouseId, ProductIds = productIds },
            cancellationToken: ct,
            transaction: unitOfWork.Transaction
        );

        var stocks = await unitOfWork.Connection.QueryAsync<Stock>(cmd);

        return Result.Success<IReadOnlyList<Stock>>(stocks.ToList());
    }

    public async Task<Result<IReadOnlyList<Guid>>> SaveRangeAsync(IReadOnlyList<Stock> stocks, CancellationToken ct)
    {
        if (stocks.Count == 0)
            return Result.Success<IReadOnlyList<Guid>>([]);

        // 帶進去的是 delta, 而不是絕對值:全新的一列就直接拿 delta 當初始值,
        // 若跟既有列衝突則對現有值做 quantity = quantity + delta 的原子更新,
        // 並在 WHERE 內檢查套用後不會變負數,不成立的那幾列就不會被更新、也不會出現在 RETURNING 裡。
        var cmd = new CommandDefinition(
            """
            insert into stocks (id, product_id, warehouse_id, quantity, cumulative_shipped,
                                 product_no, product_name, unit, warehouse_name)
            select * from unnest(
                @Ids, @ProductIds, @WarehouseIds, @QuantityDeltas, @CumulativeShippedDeltas,
                @ProductNos, @ProductNames, @Units, @WarehouseNames
            ) as t(id, product_id, warehouse_id, quantity, cumulative_shipped,
                   product_no, product_name, unit, warehouse_name)
            on conflict (product_id, warehouse_id) do update
            set quantity = stocks.quantity + excluded.quantity,
                cumulative_shipped = stocks.cumulative_shipped + excluded.cumulative_shipped,
                product_no = excluded.product_no,
                product_name = excluded.product_name,
                unit = excluded.unit,
                warehouse_name = excluded.warehouse_name
            where stocks.quantity + excluded.quantity >= 0
              and stocks.cumulative_shipped + excluded.cumulative_shipped >= 0
            returning product_id
            """,
            new
            {
                Ids = stocks.Select(s => s.Id).ToArray(),
                ProductIds = stocks.Select(s => s.ProductId).ToArray(),
                WarehouseIds = stocks.Select(s => s.WarehouseId).ToArray(),
                QuantityDeltas = stocks.Select(s => s.QuantityDelta).ToArray(),
                CumulativeShippedDeltas = stocks.Select(s => s.CumulativeShippedDelta).ToArray(),
                ProductNos = stocks.Select(s => s.ProductNo).ToArray(),
                ProductNames = stocks.Select(s => s.ProductName).ToArray(),
                Units = stocks.Select(s => s.Unit).ToArray(),
                WarehouseNames = stocks.Select(s => s.WarehouseName).ToArray()
            },
            cancellationToken: ct,
            transaction: unitOfWork.Transaction
        );

        var savedProductIds = (await unitOfWork.Connection.QueryAsync<Guid>(cmd)).ToHashSet();

        foreach (var stock in stocks.Where(s => savedProductIds.Contains(s.ProductId)))
            stock.ClearPendingChanges();

        var skippedProductIds = stocks.Select(s => s.ProductId).Where(id => !savedProductIds.Contains(id)).ToList();

        return Result.Success<IReadOnlyList<Guid>>(skippedProductIds);
    }
}
