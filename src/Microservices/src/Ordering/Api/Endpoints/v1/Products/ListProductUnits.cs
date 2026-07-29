using Api.Extension;
using Application.Products;
using Davish.Sendr;

namespace Api.Endpoints.v1.Products;

public static class ListProductUnitsEndpoint
{
    extension(RouteGroupBuilder productsV1Group)
    {
        public RouteGroupBuilder MapListProductUnitsEndpoint()
        {
            productsV1Group.MapGet("units", Handle)
                .Produces<ProductUnitsDto>()
                .WithName("ListProductUnits")
                .WithSummary("List product units")
                .WithDescription("List all product units.")
                .RequireAuthorization("AnyWarehouseRole");

            return productsV1Group;
        }
    }

    private static async Task<IResult> Handle(
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.SendAsync(new ListProductUnitsQuery(), ct);

        return result.ToOk();
    }
}
