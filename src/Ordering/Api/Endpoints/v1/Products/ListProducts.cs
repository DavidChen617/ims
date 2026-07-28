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
        int page = 1,
        int size = 20)
    {
        var result = await sender.SendAsync(new ListProductsQuery(page, size), ct);

        return result.ToOk();
    }
}
