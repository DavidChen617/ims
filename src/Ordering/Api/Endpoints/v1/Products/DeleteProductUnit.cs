using Api.Extension;
using Application.Products;
using Davish.Sendr;

namespace Api.Endpoints.v1.Products;

public static class DeleteProductUnitEndpoint
{
    extension(RouteGroupBuilder productsV1Group)
    {
        public RouteGroupBuilder MapDeleteProductUnitEndpoint()
        {
            productsV1Group.MapDelete("units/{name}", Handle)
                .Produces(StatusCodes.Status404NotFound)
                .Produces(StatusCodes.Status409Conflict)
                .WithName("DeleteProductUnit")
                .WithSummary("Delete a product unit")
                .WithDescription("Delete a product unit. Returns 409 if the unit is still used by a product.")
                .RequireAuthorization("AdminOrWarehouseAdmin");

            return productsV1Group;
        }
    }

    private static async Task<IResult> Handle(
        string name,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.SendAsync(new DeleteProductUnitCommand(name), ct);

        return result.ToNoContent();
    }
}
