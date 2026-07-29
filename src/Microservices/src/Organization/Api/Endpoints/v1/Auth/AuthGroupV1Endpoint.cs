using Api.Endpoints.Filter;

namespace Api.Endpoints.v1.Auth;

public static class AuthGroupV1Endpoint
{
    extension(RouteGroupBuilder groupBuilder)
    {
        public void MapAuthV1Endpoints()
        {
            var authV1 = groupBuilder.MapGroup("auth")
                .HasApiVersion(1)
                .WithTags("AuthV1")
                .AddEndpointFilter<TransactionFilter>();

            authV1.MapLoginEndpoint()
                .MapLogoutEndpoint()
                .MapRefreshTokenEndpoint()
                .MapRegisterWarehouseUserEndpoint()
                .MapRegisterUserEndpoint();
        }
    }
}
