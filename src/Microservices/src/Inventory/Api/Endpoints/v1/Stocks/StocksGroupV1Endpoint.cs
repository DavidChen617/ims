namespace Api.Endpoints.v1.Stocks;

public static class StocksGroupV1Endpoint
{
    extension(RouteGroupBuilder groupBuilder)
    {
        public void MapStocksV1Endpoints()
        {
            var stocksV1 = groupBuilder.MapGroup("stocks")
                .HasApiVersion(1)
                .WithTags("StocksV1");

            stocksV1
                .MapListStocksEndpoint()
                .MapListWarehouseStocksEndpoint();
        }
    }
}
