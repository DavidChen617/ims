using Api.Extension;
using Application.Products;
using Davish.Sendr;

namespace Api.Endpoints.v1.Products;

public static class CreateProductEndpoint
{
    extension(RouteGroupBuilder productsV1Group)
    {
        public RouteGroupBuilder MapCreateProductEndpoint()
        {
            productsV1Group.MapPost("", Handle)
                .Produces<CreateProductDto>()
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status409Conflict)
                .WithName("CreateProduct")
                .WithSummary("Create a product")
                .WithDescription("Create a new product with a unique product number and an existing unit.")
                .RequireAuthorization("AdminOrWarehouseAdmin");

            return productsV1Group;
        }
    }

    private static async Task<IResult> Handle(
        CreateProductCommand command,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.SendAsync(command, ct);

        return result.ToCreatedAtRoute("GetProduct", x => new { id = x.ProductId });
    }
}
