using Dapper;
using Davish.Result;
using Domain.Products;
using SharedKernel;

namespace Infrastructure.Persistence.Repositories;

public sealed class ProductRepository(
    IOrderingUnitOfWork unitOfWork,
    IAggregateRootChangeTracker tracker
) : IProductRepository
{
    public async Task<Result> AddAsync(Product product, CancellationToken ct)
    {
        var cmd = new CommandDefinition(
            """
            insert into products(id, product_no, name, unit, price)
            values(@Id, @ProductNo, @Name, @Unit, @Price)
            """,
            product,
            cancellationToken: ct,
            transaction: unitOfWork.Transaction
        );

        await unitOfWork.Connection.ExecuteAsync(cmd);
        tracker.Enqueue(product);

        return Result.Success();
    }

    public async Task<Result<Product>> GetByNoAsync(string productNo, CancellationToken ct)
    {
        var cmd = new CommandDefinition(
            """
            select id, product_no, name, unit, price
            from products
            where product_no = @ProductNo
            """,
            new { ProductNo = productNo },
            cancellationToken: ct,
            transaction: unitOfWork.Transaction
        );

        var product = await unitOfWork.Connection.QuerySingleOrDefaultAsync<Product>(cmd);

        return product is null
            ? new Error("Product.NotFound", "Product not found", ErrorType.NotFound)
            : product;
    }

    public async Task<Result<IReadOnlyList<Guid>>> GetExistingIdsAsync(IReadOnlyList<Guid> ids, CancellationToken ct)
    {
        var cmd = new CommandDefinition(
            """
            select id
            from products
            where id = any(@Ids)
            """,
            new { Ids = ids },
            cancellationToken: ct,
            transaction: unitOfWork.Transaction
        );

        var existingIds = await unitOfWork.Connection.QueryAsync<Guid>(cmd);

        return Result.Success<IReadOnlyList<Guid>>(existingIds.ToList());
    }

    public async Task<Result<IReadOnlyList<Product>>> GetByIdsAsync(IReadOnlyList<Guid> ids, CancellationToken ct)
    {
        var cmd = new CommandDefinition(
            """
            select id, product_no, name, unit, price
            from products
            where id = any(@Ids)
            """,
            new { Ids = ids },
            cancellationToken: ct,
            transaction: unitOfWork.Transaction
        );

        var products = await unitOfWork.Connection.QueryAsync<Product>(cmd);

        return Result.Success<IReadOnlyList<Product>>(products.ToList());
    }

    public async Task<Result> AddUnitAsync(ProductUnit unit, CancellationToken ct)
    {
        var cmd = new CommandDefinition(
            """
            insert into product_units(name)
            values(@Name)
            """,
            unit,
            cancellationToken: ct,
            transaction: unitOfWork.Transaction
        );

        await unitOfWork.Connection.ExecuteAsync(cmd);

        return Result.Success();
    }

    public async Task<Result> DeleteUnitAsync(string name, CancellationToken ct)
    {
        var cmd = new CommandDefinition(
            """
            delete from product_units
            where name = @Name
            """,
            new { Name = name },
            cancellationToken: ct,
            transaction: unitOfWork.Transaction
        );

        await unitOfWork.Connection.ExecuteAsync(cmd);

        return Result.Success();
    }

    public async Task<Result<ProductUnit>> GetUnitByNameAsync(string name, CancellationToken ct)
    {
        var cmd = new CommandDefinition(
            """
            select name
            from product_units
            where name = @Name
            """,
            new { Name = name },
            cancellationToken: ct,
            transaction: unitOfWork.Transaction
        );

        var unit = await unitOfWork.Connection.QuerySingleOrDefaultAsync<ProductUnit>(cmd);

        return unit is null
            ? new Error("ProductUnit.NotFound", "Unit not found", ErrorType.NotFound)
            : unit;
    }

    public async Task<Result<bool>> IsUnitInUseAsync(string name, CancellationToken ct)
    {
        var cmd = new CommandDefinition(
            """
            select exists(select 1 from products where unit = @Name)
            """,
            new { Name = name },
            cancellationToken: ct,
            transaction: unitOfWork.Transaction
        );

        var inUse = await unitOfWork.Connection.ExecuteScalarAsync<bool>(cmd);

        return inUse;
    }
}
