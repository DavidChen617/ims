using Api.Endpoints.v1.Auth;
using Api.Endpoints.v1.Users;
using Api.Endpoints.v1.Warehouse;

namespace Api.Endpoints.v1;

public static class MapV1Endpoint
{
    extension(RouteGroupBuilder groupBuilder)
    {
        public void MapV1Endpoints()
        {
            groupBuilder.MapAuthV1Endpoints();
            groupBuilder.MapWarehouseV1Endpoints();
            groupBuilder.MapUsersV1Endpoints();
        }
    }
}
