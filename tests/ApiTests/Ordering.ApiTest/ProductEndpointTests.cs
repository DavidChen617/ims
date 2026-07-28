using System.Net;
using System.Net.Http.Headers;
using Application.Products;
using Npgsql;

namespace Ordering.ApiTest;

public class ProductEndpointTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private HttpClient CreateClient(string? role, Guid? warehouseId = null)
    {
        var client = factory.CreateClient();

        if (role is null)
        {
            return client;
        }

        var token = TestJwt.Create(factory.SigningKey, role, warehouseId);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return client;
    }

    private async Task DeleteProductAsync(Guid id)
    {
        await using var connection = new NpgsqlConnection(factory.ConnectionString);
        await connection.OpenAsync();
        await using var cmd = new NpgsqlCommand("delete from products where id = @Id", connection);
        cmd.Parameters.AddWithValue("Id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task DeleteUnitAsync(string name)
    {
        await using var connection = new NpgsqlConnection(factory.ConnectionString);
        await connection.OpenAsync();
        await using var cmd = new NpgsqlCommand("delete from product_units where name = @Name", connection);
        cmd.Parameters.AddWithValue("Name", name);
        await cmd.ExecuteNonQueryAsync();
    }

    [Fact]
    public async Task GivenNoToken_WhenCreatingProduct_ThenReturnsUnauthorized()
    {
        var client = CreateClient(role: null);

        var response = await client.PostAsJsonAsync("/api/v1/products",
            new CreateProductCommand($"P-{Guid.CreateVersion7()}", "Test Product", "pcs", 1m));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GivenWarehouseUserToken_WhenCreatingProduct_ThenReturnsForbidden()
    {
        var client = CreateClient("WarehouseUser", Guid.CreateVersion7());

        var response = await client.PostAsJsonAsync("/api/v1/products",
            new CreateProductCommand($"P-{Guid.CreateVersion7()}", "Test Product", "pcs", 1m));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GivenWarehouseAdminToken_WhenCreatingUnitThenProductThenGettingIt_ThenReturnsTheSameProduct()
    {
        var client = CreateClient("WarehouseAdmin", Guid.CreateVersion7());
        var unitName = $"unit-{Guid.CreateVersion7()}";
        var productNo = $"P-{Guid.CreateVersion7()}";

        Guid? productId = null;

        try
        {
            var unitResponse = await client.PostAsJsonAsync("/api/v1/products/units", new CreateProductUnitCommand(unitName));
            Assert.Equal(HttpStatusCode.Created, unitResponse.StatusCode);

            var createResponse = await client.PostAsJsonAsync("/api/v1/products",
                new CreateProductCommand(productNo, "Test Product", unitName, 9.9m));
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

            // CreateProduct 的 ToCreatedAtRoute(routeName, routeValues) 這個 overload 不會回傳
            // body —— 只有一個指向 GetProduct 的 Location header,所以 id 要從那裡拿。
            var location = createResponse.Headers.Location;
            Assert.NotNull(location);
            productId = Guid.Parse(location!.ToString().Split('/')[^1]);

            var getResponse = await client.GetAsync(location);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            var product = await getResponse.Content.ReadFromJsonAsync<ProductDto>();

            Assert.NotNull(product);
            Assert.Equal(productNo, product!.ProductNo);
            Assert.Equal(unitName, product.Unit);
        }
        finally
        {
            // 刪除順序有關係:products.unit 有外鍵指向 product_units,所以不管上面哪個
            // 斷言拋了例外,只要商品真的建立成功就要先刪商品。
            if (productId is not null)
                await DeleteProductAsync(productId.Value);

            await DeleteUnitAsync(unitName);
        }
    }

    [Fact]
    public async Task GivenWarehouseUserToken_WhenListingProducts_ThenReturnsOk()
    {
        var client = CreateClient("WarehouseUser", Guid.CreateVersion7());

        var response = await client.GetAsync("/api/v1/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
