using Davish.Result;

namespace Application.Products;

public sealed record ListProductUnitsQuery : IQuery<Result<ProductUnitsDto>>;

public sealed record ProductUnitDto(string Name);

public sealed record ProductUnitsDto(IReadOnlyList<ProductUnitDto> Items);

public sealed class ListProductUnitsQueryHandler(
    IProductReader reader
) : IQueryHandler<ListProductUnitsQuery, Result<ProductUnitsDto>>
{
    public async Task<Result<ProductUnitsDto>> HandleAsync(
        ListProductUnitsQuery request, CancellationToken cancellationToken)
    {
        return await reader.ListUnitsAsync(cancellationToken)
            .Then(items => new ProductUnitsDto(items));
    }
}
