using Davish.Result;
using Domain.Products;

namespace Application.Products;

public sealed record DeleteProductUnitCommand(
    string Name
) : ICommand<Result>;

public sealed class DeleteProductUnitCommandHandler(
    IProductRepository repository
) : ICommandHandler<DeleteProductUnitCommand, Result>
{
    public async Task<Result> HandleAsync(DeleteProductUnitCommand request, CancellationToken cancellationToken)
    {
        var isUnitInUseResult = await repository
            .GetUnitByNameAsync(request.Name, cancellationToken)
            .ThenAsync(async _ =>
                await repository.IsUnitInUseAsync(request.Name, cancellationToken));

        if (!isUnitInUseResult.IsSuccess)
            return isUnitInUseResult.Error;

        if (isUnitInUseResult.Value)
            return new Error("ProductUnit.Delete", "Unit is already in use", ErrorType.Conflict);

        return await repository.DeleteUnitAsync(request.Name, cancellationToken);
    }
}
