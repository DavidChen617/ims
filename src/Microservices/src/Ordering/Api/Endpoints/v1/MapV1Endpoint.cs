using Api.Endpoints.v1.Inbound;
using Api.Endpoints.v1.Outbound;
using Api.Endpoints.v1.Products;

namespace Api.Endpoints.v1;

public static class MapV1Endpoint
{
    extension(RouteGroupBuilder groupBuilder)
    {
        public void MapV1Endpoints()
        {
            groupBuilder.MapInboundV1Endpoints();
            groupBuilder.MapOutboundV1Endpoints();
            groupBuilder.MapProductsV1Endpoints();
        }
    }
}
