using Api.Extension;
using Application.Products;
using Davish.Sendr;

namespace Api.Endpoints.v1.Products;

public static class CreateProductUnitEndpoint
{
    extension(RouteGroupBuilder productsV1Group)
    {
        public RouteGroupBuilder MapCreateProductUnitEndpoint()
        {
            productsV1Group.MapPost("units", Handle)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status409Conflict)
                .WithName("CreateProductUnit")
                .WithSummary("Create a product unit")
                .WithDescription("Create a new product unit with a unique name.")
                .RequireAuthorization("AdminOrWarehouseAdmin");

            return productsV1Group;
        }
    }

    private static async Task<IResult> Handle(
        CreateProductUnitCommand command,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.SendAsync(command, ct);

        return result.ToCreated(new Uri($"/api/v1/products/units/{command.Name}", UriKind.Relative));
    }
}
