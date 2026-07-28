using Dapper;
using Davish.Result;
using Domain.Stocks;
using Infrastructure.Persistence;

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

        return Result.Success();
    }

    public async Task<Result> SaveAsync(Stock stock, CancellationToken ct)
    {
        var cmd = new CommandDefinition(
            """
            update stocks
            set quantity = @Quantity, cumulative_shipped = @CumulativeShipped,
                product_no = @ProductNo, product_name = @ProductName,
                unit = @Unit, warehouse_name = @WarehouseName
            where id = @Id
            """,
            stock,
            cancellationToken: ct,
            transaction: unitOfWork.Transaction
        );

        await unitOfWork.Connection.ExecuteAsync(cmd);

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

    public async Task<Result> SaveRangeAsync(IReadOnlyList<Stock> stocks, CancellationToken ct)
    {
        if (stocks.Count == 0)
            return Result.Success();

        // 用 unnest 把整批 Stock 攤平成多欄陣列參數,一次 insert...on conflict do update
        // 取代逐筆的 AddAsync/SaveAsync —— 不用先分好哪些是新增、哪些是更新,
        // (product_id, warehouse_id) 的 unique constraint 交給資料庫自己判斷。
        var cmd = new CommandDefinition(
            """
            insert into stocks (id, product_id, warehouse_id, quantity, cumulative_shipped,
                                 product_no, product_name, unit, warehouse_name)
            select * from unnest(
                @Ids, @ProductIds, @WarehouseIds, @Quantities, @CumulativeShippeds,
                @ProductNos, @ProductNames, @Units, @WarehouseNames
            ) as t(id, product_id, warehouse_id, quantity, cumulative_shipped,
                   product_no, product_name, unit, warehouse_name)
            on conflict (product_id, warehouse_id) do update
            set quantity = excluded.quantity,
                cumulative_shipped = excluded.cumulative_shipped,
                product_no = excluded.product_no,
                product_name = excluded.product_name,
                unit = excluded.unit,
                warehouse_name = excluded.warehouse_name
            """,
            new
            {
                Ids = stocks.Select(s => s.Id).ToArray(),
                ProductIds = stocks.Select(s => s.ProductId).ToArray(),
                WarehouseIds = stocks.Select(s => s.WarehouseId).ToArray(),
                Quantities = stocks.Select(s => s.Quantity).ToArray(),
                CumulativeShippeds = stocks.Select(s => s.CumulativeShipped).ToArray(),
                ProductNos = stocks.Select(s => s.ProductNo).ToArray(),
                ProductNames = stocks.Select(s => s.ProductName).ToArray(),
                Units = stocks.Select(s => s.Unit).ToArray(),
                WarehouseNames = stocks.Select(s => s.WarehouseName).ToArray()
            },
            cancellationToken: ct,
            transaction: unitOfWork.Transaction
        );

        await unitOfWork.Connection.ExecuteAsync(cmd);

        return Result.Success();
    }
}
