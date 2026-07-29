namespace Api.Endpoints.v1.Warehouse;

public static class WarehouseGroupV1Endpoint
{
    extension(RouteGroupBuilder groupBuilder)
    {
        public void MapWarehouseV1Endpoints()
        {
            var warehouseV1 = groupBuilder.MapGroup("warehouse")
                .HasApiVersion(1)
                .WithTags("WarehouseV1");

            warehouseV1
                .MapCreateWarehouseEndpoint()
                .MapListWarehouseEndpoint()
                .MapGetWarehouseEndpoint();
        }
    }
}
