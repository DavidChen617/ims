using Application;
using Application.Stocks;
using Dapper;
using Davish.Result;

namespace Infrastructure.Persistence.Readers;

public sealed class StockReader(IInventoryUnitOfWork unitOfWork) : IStockReader
{
    public async Task<Result<PagedResult<StockDto>>> ListAsync(
        Guid? warehouseId, Guid? productId, string? productNo, string? productName, string? unit,
        int? quantityMin, int? quantityMax, int? cumulativeShippedMin, int? cumulativeShippedMax,
        int page, int size, CancellationToken ct)
    {
        var parameters = new
        {
            WarehouseId = warehouseId,
            ProductId = productId,
            ProductNo = productNo is null ? null : $"%{productNo}%",
            ProductName = productName is null ? null : $"%{productName}%",
            Unit = unit,
            QuantityMin = quantityMin,
            QuantityMax = quantityMax,
            CumulativeShippedMin = cumulativeShippedMin,
            CumulativeShippedMax = cumulativeShippedMax,
            Size = size,
            Offset = (page - 1) * size
        };

        var countCmd = new CommandDefinition(
            """
            select count(*)
            from stocks
            where (@WarehouseId::uuid is null or warehouse_id = @WarehouseId::uuid)
              and (@ProductId::uuid is null or product_id = @ProductId::uuid)
              and (@ProductNo is null or product_no ilike @ProductNo)
              and (@ProductName is null or product_name ilike @ProductName)
              and (@Unit is null or unit = @Unit)
              and (@QuantityMin::int is null or quantity >= @QuantityMin::int)
              and (@QuantityMax::int is null or quantity <= @QuantityMax::int)
              and (@CumulativeShippedMin::int is null or cumulative_shipped >= @CumulativeShippedMin::int)
              and (@CumulativeShippedMax::int is null or cumulative_shipped <= @CumulativeShippedMax::int)
            """,
            parameters,
            cancellationToken: ct,
            transaction: unitOfWork.Transaction
        );

        var totalCount = await unitOfWork.Connection.ExecuteScalarAsync<int>(countCmd);

        var listCmd = new CommandDefinition(
            """
            select product_id, product_no, product_name, unit, warehouse_id, warehouse_name,
                   quantity, cumulative_shipped
            from stocks
            where (@WarehouseId::uuid is null or warehouse_id = @WarehouseId::uuid)
              and (@ProductId::uuid is null or product_id = @ProductId::uuid)
              and (@ProductNo is null or product_no ilike @ProductNo)
              and (@ProductName is null or product_name ilike @ProductName)
              and (@Unit is null or unit = @Unit)
              and (@QuantityMin::int is null or quantity >= @QuantityMin::int)
              and (@QuantityMax::int is null or quantity <= @QuantityMax::int)
              and (@CumulativeShippedMin::int is null or cumulative_shipped >= @CumulativeShippedMin::int)
              and (@CumulativeShippedMax::int is null or cumulative_shipped <= @CumulativeShippedMax::int)
            order by product_id, warehouse_id
            limit @Size offset @Offset
            """,
            parameters,
            cancellationToken: ct,
            transaction: unitOfWork.Transaction
        );

        var stocks = await unitOfWork.Connection.QueryAsync<StockDto>(listCmd);

        return new PagedResult<StockDto>(stocks.ToList(), totalCount, page, size);
    }
}
