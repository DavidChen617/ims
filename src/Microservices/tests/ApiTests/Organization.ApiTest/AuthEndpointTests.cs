using System.Net;
using Api.Endpoints.v1.Auth;

namespace Organization.ApiTest;

public class AuthEndpointTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task GivenSeededAdmin_WhenLoggingInWithCorrectPassword_ThenReturnsOkWithAccessToken()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest("admin", "1qazXSW@"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LoginDto>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.AccessToken));
    }

    [Fact]
    public async Task GivenSeededAdmin_WhenLoggingInWithWrongPassword_ThenReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest("admin", "wrong-password"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
   }

    [Fact]
    public async Task GivenUnknownUsername_WhenLoggingIn_ThenReturnsUnauthorizedOrNotFound()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest($"no-such-user-{Guid.CreateVersion7()}", "irrelevant"));

        Assert.False(response.IsSuccessStatusCode);
    }
}
