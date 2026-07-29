namespace Api.Endpoints.v1.Products;

public static class ProductsGroupV1Endpoint
{
    extension(RouteGroupBuilder groupBuilder)
    {
        public void MapProductsV1Endpoints()
        {
            var productsV1 = groupBuilder.MapGroup("products")
                .HasApiVersion(1)
                .WithTags("ProductsV1");

            productsV1
                .MapCreateProductEndpoint()
                .MapGetProductEndpoint()
                .MapListProductsEndpoint()
                .MapCreateProductUnitEndpoint()
                .MapDeleteProductUnitEndpoint()
                .MapListProductUnitsEndpoint();
        }
    }
}
