using Davish.Result;

namespace Application.Products;

public sealed record GetProductQuery(Guid Id) : IQuery<Result<ProductDto>>;

public sealed record ProductDto(Guid Id, string ProductNo, string Name, string Unit, decimal Price);

public sealed class GetProductQueryHandler(
    IProductReader reader
) : IQueryHandler<GetProductQuery, Result<ProductDto>>
{
    public async Task<Result<ProductDto>> HandleAsync(GetProductQuery request, CancellationToken cancellationToken)
    {
        return await reader.GetByIdAsync(request.Id, cancellationToken);
    }
}
