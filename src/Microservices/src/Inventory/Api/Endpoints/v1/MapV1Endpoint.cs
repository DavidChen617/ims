using Api.Endpoints.v1.Stocks;

namespace Api.Endpoints.v1;

public static class MapV1Endpoint
{
    extension(RouteGroupBuilder groupBuilder)
    {
        public void MapV1Endpoints()
        {
            groupBuilder.MapStocksV1Endpoints();
        }
    }
}
