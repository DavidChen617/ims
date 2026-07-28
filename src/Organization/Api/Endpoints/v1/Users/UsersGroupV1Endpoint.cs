namespace Api.Endpoints.v1.Users;

public static class UsersGroupV1Endpoint
{
    extension(RouteGroupBuilder groupBuilder)
    {
        public void MapUsersV1Endpoints()
        {
            var usersV1 = groupBuilder.MapGroup("users")
                .HasApiVersion(1)
                .WithTags("UsersV1");

            usersV1
                .MapListUserEndpoint()
                .MapListWarehouseUserEndpoint();
        }
    }
}
