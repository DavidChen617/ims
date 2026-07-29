using Api.Extension;
using Application;
using Application.Products;
using Davish.Sendr;

namespace Api.Endpoints.v1.Products;

public static class ListProductsEndpoint
{
    extension(RouteGroupBuilder productsV1Group)
    {
        public RouteGroupBuilder MapListProductsEndpoint()
        {
            productsV1Group.MapGet("", Handle)
                .Produces<PagedResult<ProductDto>>()
                .WithName("ListProducts")
                .WithSummary("List products")
                .WithDescription("List products with pagination.")
                .RequireAuthorization("AnyWarehouseRole");

            return productsV1Group;
        }
    }

    private static async Task<IResult> Handle(
        ISender sender,
        CancellationToken ct,
        [AsParameters] ListProductsRequest request)
    {
        var result = await sender.SendAsync(
            new ListProductsQuery(
                request.ProductNo, request.Name, request.Unit, request.PriceMin, request.PriceMax,
                request.Page ?? 1, request.Size ?? 20),
            ct);

        return result.ToOk();
    }
}

public sealed record ListProductsRequest(
    string? ProductNo = null,
    string? Name = null,
    string? Unit = null,
    decimal? PriceMin = null,
    decimal? PriceMax = null,
    int? Page = null,
    int? Size = null);
