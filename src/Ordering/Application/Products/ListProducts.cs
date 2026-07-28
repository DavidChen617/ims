using Davish.Result;

namespace Application.Products;

public sealed record ListProductsQuery(int Page, int Size) : IQuery<Result<PagedResult<ProductDto>>>;

public sealed class ListProductsQueryHandler(
    IProductReader reader
) : IQueryHandler<ListProductsQuery, Result<PagedResult<ProductDto>>>
{
    public async Task<Result<PagedResult<ProductDto>>> HandleAsync(
        ListProductsQuery request, CancellationToken cancellationToken)
    {
        return await reader.ListAsync(request.Page, request.Size, cancellationToken);
    }
}
