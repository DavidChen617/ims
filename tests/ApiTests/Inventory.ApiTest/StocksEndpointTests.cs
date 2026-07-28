using System.Net;
using System.Net.Http.Headers;

namespace Inventory.ApiTest;

public class StocksEndpointTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private HttpClient CreateClient(string? role, Guid? warehouseId = null)
    {
        var client = factory.CreateClient();

        if (role is not null)
        {
            var token = TestJwt.Create(factory.SigningKey, role, warehouseId);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return client;
    }

    [Fact]
    public async Task GivenNoToken_WhenListingStocks_ThenReturnsUnauthorized()
    {
        var client = CreateClient(role: null);

        var response = await client.GetAsync("/api/v1/stocks");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GivenWarehouseUserToken_WhenListingStocks_ThenReturnsForbidden()
    {
        var client = CreateClient("WarehouseUser", Guid.CreateVersion7());

        var response = await client.GetAsync("/api/v1/stocks");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GivenAdminToken_WhenListingStocks_ThenReturnsOk()
    {
        var client = CreateClient("Admin");

        var response = await client.GetAsync("/api/v1/stocks");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GivenAdminToken_WhenListingWarehouseStocks_ThenReturnsForbidden()
    {
        var client = CreateClient("Admin");

        var response = await client.GetAsync("/api/v1/stocks/warehouse");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GivenWarehouseAdminToken_WhenListingWarehouseStocks_ThenReturnsOk()
    {
        var client = CreateClient("WarehouseAdmin", Guid.CreateVersion7());

        var response = await client.GetAsync("/api/v1/stocks/warehouse");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
