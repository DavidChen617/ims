using Davish.Result;
using Domain.Products;

namespace Application.Products;

public sealed record CreateProductCommand(
    string ProductNo,
    string Name,
    string Unit,
    decimal Price
) : ICommand<Result<CreateProductDto>>;

public sealed record CreateProductDto(Guid ProductId);

public sealed class CreateProductCommandHandler(
    IProductRepository repository
) : ICommandHandler<CreateProductCommand, Result<CreateProductDto>>
{
    public async Task<Result<CreateProductDto>> HandleAsync(
        CreateProductCommand request, CancellationToken cancellationToken)
    {
        var existing = await repository.GetByNoAsync(request.ProductNo, cancellationToken);

        if (existing.IsSuccess)
            return new Error("Product.Create", "Product no already exists", ErrorType.Conflict);

        return await repository
            .GetUnitByNameAsync(request.Unit, cancellationToken)
            .Then(unit => Product.Create(request.ProductNo, request.Name, unit, request.Price))
            .ThenAsync(async product =>
            {
                await repository.AddAsync(product, cancellationToken);

                return Result.Success(new CreateProductDto(product.Id));
            });
    }
}
