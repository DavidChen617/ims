using System.Net;
using System.Net.Http.Headers;
using Api.Endpoints.v1.Auth;
using Api.Endpoints.v1.Warehouse;
using Npgsql;

namespace Organization.ApiTest;

public class WarehouseEndpointTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("admin", "1qazXSW@"));
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginDto>();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.AccessToken);
        return client;
    }

    private async Task DeleteWarehouseAsync(Guid id)
    {
        await using var connection = new NpgsqlConnection(factory.ConnectionString);
        await connection.OpenAsync();
        await using var cmd = new NpgsqlCommand("delete from warehouse where id = @Id", connection);
        cmd.Parameters.AddWithValue("Id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    [Fact]
    public async Task GivenAdminToken_WhenCreatingWarehouseThenListing_ThenTheNewWarehouseIsReturned()
    {
        var client = await CreateAuthenticatedClientAsync();
        var name = $"warehouse-{Guid.CreateVersion7()}";

        var createResponse = await client.PostAsJsonAsync("/api/v1/warehouse", new CreateWarehouseRequest(name));
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<WarehouseDto>();
        Assert.NotNull(created);

        try
        {
            var listResponse = await client.GetAsync("/api/v1/warehouse");
            Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
            var list = await listResponse.Content.ReadFromJsonAsync<WarehousesDto>();

            Assert.NotNull(list);
            Assert.Contains(list!.Items, w => w.Id == created!.Id && w.Name == name);
        }
        finally
        {
            await DeleteWarehouseAsync(created!.Id);
        }
    }

    [Fact]
    public async Task GivenDuplicateName_WhenCreatingWarehouse_ThenReturnsBadRequest()
    {
        var client = await CreateAuthenticatedClientAsync();
        var name = $"warehouse-{Guid.CreateVersion7()}";

        var firstResponse = await client.PostAsJsonAsync("/api/v1/warehouse", new CreateWarehouseRequest(name));
        var created = await firstResponse.Content.ReadFromJsonAsync<WarehouseDto>();

        try
        {
            var secondResponse = await client.PostAsJsonAsync("/api/v1/warehouse", new CreateWarehouseRequest(name));

            Assert.Equal(HttpStatusCode.BadRequest, secondResponse.StatusCode);
        }
        finally
        {
            await DeleteWarehouseAsync(created!.Id);
        }
    }

    [Fact]
    public async Task GivenNoBearerToken_WhenCreatingWarehouse_ThenReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/warehouse",
            new CreateWarehouseRequest($"warehouse-{Guid.CreateVersion7()}"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
