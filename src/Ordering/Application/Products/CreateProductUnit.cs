using Davish.Result;
using Domain.Products;

namespace Application.Products;

public sealed record CreateProductUnitCommand(
    string Name
) : ICommand<Result>;

public sealed class CreateProductUnitCommandHandler(
    IProductRepository repository
) : ICommandHandler<CreateProductUnitCommand, Result>
{
    public async Task<Result> HandleAsync(
        CreateProductUnitCommand request, CancellationToken cancellationToken)
    {
        var existing = await repository.GetUnitByNameAsync(request.Name, cancellationToken);

        if (existing.IsSuccess)
            return new Error("ProductUnit.Create", "Unit already exists", ErrorType.Conflict);

        return await ProductUnit.Create(request.Name)
            .ThenAsync(async unit =>
            {
                await repository.AddUnitAsync(unit, cancellationToken);

                return Result.Success();
            });
    }
}
