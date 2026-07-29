using Application;
using Application.Products;
using Dapper;
using Davish.Result;

namespace Infrastructure.Persistence.Readers;

public sealed class ProductReader(IOrderingUnitOfWork unitOfWork) : IProductReader
{
    public async Task<Result<ProductDto>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var cmd = new CommandDefinition(
            """
            select id, product_no, name, unit, price
            from products
            where id = @Id
            """,
            new { Id = id },
            cancellationToken: ct,
            transaction: unitOfWork.Transaction
        );

        var product = await unitOfWork.Connection.QuerySingleOrDefaultAsync<ProductDto>(cmd);

        return product is null
            ? new Error("Product.NotFound", "Product not found", ErrorType.NotFound)
            : product;
    }

    public async Task<Result<PagedResult<ProductDto>>> ListAsync(
        string? productNo, string? name, string? unit, decimal? priceMin, decimal? priceMax,
        int page, int size, CancellationToken ct)
    {
        var filters = new
        {
            ProductNo = productNo,
            Name = name,
            Unit = unit,
            PriceMin = priceMin,
            PriceMax = priceMax,
        };

        var countCmd = new CommandDefinition(
            """
            select count(*)
            from products
            where (@ProductNo is null or product_no ilike '%' || @ProductNo || '%')
              and (@Name is null or name ilike '%' || @Name || '%')
              and (@Unit is null or unit = @Unit)
              and (@PriceMin::numeric is null or price >= @PriceMin::numeric)
              and (@PriceMax::numeric is null or price <= @PriceMax::numeric)
            """,
            filters,
            cancellationToken: ct,
            transaction: unitOfWork.Transaction
        );

        var totalCount = await unitOfWork.Connection.ExecuteScalarAsync<int>(countCmd);

        var listCmd = new CommandDefinition(
            """
            select id, product_no, name, unit, price
            from products
            where (@ProductNo is null or product_no ilike '%' || @ProductNo || '%')
              and (@Name is null or name ilike '%' || @Name || '%')
              and (@Unit is null or unit = @Unit)
              and (@PriceMin::numeric is null or price >= @PriceMin::numeric)
              and (@PriceMax::numeric is null or price <= @PriceMax::numeric)
            order by product_no
            limit @Size offset @Offset
            """,
            new
            {
                filters.ProductNo, filters.Name, filters.Unit, filters.PriceMin, filters.PriceMax,
                Size = size, Offset = (page - 1) * size,
            },
            cancellationToken: ct,
            transaction: unitOfWork.Transaction
        );

        var products = await unitOfWork.Connection.QueryAsync<ProductDto>(listCmd);

        return new PagedResult<ProductDto>(products.ToList(), totalCount, page, size);
    }

    public async Task<Result<IReadOnlyList<ProductUnitDto>>> ListUnitsAsync(CancellationToken ct)
    {
        var cmd = new CommandDefinition(
            """
            select name
            from product_units
            """,
            cancellationToken: ct,
            transaction: unitOfWork.Transaction
        );

        var units = await unitOfWork.Connection.QueryAsync<ProductUnitDto>(cmd);

        return Result.Success<IReadOnlyList<ProductUnitDto>>(units.ToList());
    }
}
