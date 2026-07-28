using Api.Extension;
using Application.Products;
using Davish.Sendr;

namespace Api.Endpoints.v1.Products;

public static class GetProductEndpoint
{
    extension(RouteGroupBuilder productsV1Group)
    {
        public RouteGroupBuilder MapGetProductEndpoint()
        {
            productsV1Group.MapGet("{id:guid}", Handle)
                .Produces<ProductDto>()
                .ProducesProblem(StatusCodes.Status404NotFound)
                .WithName("GetProduct")
                .WithSummary("Get a product")
                .WithDescription("Get a product by id.")
                .RequireAuthorization("AnyWarehouseRole");

            return productsV1Group;
        }
    }

    private static async Task<IResult> Handle(
        Guid id,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.SendAsync(new GetProductQuery(id), ct);

        return result.ToOk();
    }
}
