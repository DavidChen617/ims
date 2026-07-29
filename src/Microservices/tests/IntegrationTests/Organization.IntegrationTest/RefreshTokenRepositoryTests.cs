using Dapper;
using Domain.RefreshToken;
using Domain.Users;
using Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Organization.IntegrationTest;

public class RefreshTokenRepositoryTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private static async Task<User> CreatePersistedUserAsync(IUserRepository userRepository)
    {
        var user = User.RegisterAdmin("Test Admin", $"user-{Guid.CreateVersion7()}", "hashed-password", Role.Admin)
            .Value;
        await userRepository.AddAsync(user, CancellationToken.None);
        return user;
    }

    [Fact]
    public async Task GivenNewRefreshToken_WhenAddedThenQueriedByToken_ThenReturnsTheSameToken()
    {
        using var scope = factory.Services.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IOrganizationUnitOfWork>();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var refreshTokenRepository = scope.ServiceProvider.GetRequiredService<IRefreshTokenRepository>();

        var user = await CreatePersistedUserAsync(userRepository);
        var token = RefreshToken.Create($"token-{Guid.CreateVersion7()}", user.Id, DateTime.UtcNow.AddDays(7)).Value;

        try
        {
            await refreshTokenRepository.AddAsync(token, CancellationToken.None);
            var found = await refreshTokenRepository.GetByTokenAsync(token.Token, CancellationToken.None);

            Assert.True(found.IsSuccess);
            Assert.Equal(token.Id, found.Value.Id);
            Assert.Equal(user.Id, found.Value.UserId);
            Assert.False(found.Value.IsExpired);
            Assert.False(found.Value.IsRevoked);
        }
        finally
        {
            await unitOfWork.Connection.ExecuteAsync("delete from refresh_token where id = @Id", new { token.Id });
            await unitOfWork.Connection.ExecuteAsync("delete from users where id = @Id", new { user.Id });
        }
    }

    [Fact]
    public async Task GivenExistingRefreshToken_WhenRevokedAndReplacedThenSaved_ThenChangesArePersisted()
    {
        using var scope = factory.Services.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IOrganizationUnitOfWork>();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var refreshTokenRepository = scope.ServiceProvider.GetRequiredService<IRefreshTokenRepository>();

        var user = await CreatePersistedUserAsync(userRepository);
        var token = RefreshToken.Create($"token-{Guid.CreateVersion7()}", user.Id, DateTime.UtcNow.AddDays(7)).Value;
        var newTokenValue = $"token-{Guid.CreateVersion7()}";

        try
        {
            await refreshTokenRepository.AddAsync(token, CancellationToken.None);

            token.Revoke();
            token.ReplaceToken(newTokenValue);
            await refreshTokenRepository.SaveAsync(token, CancellationToken.None);

            var found = await refreshTokenRepository.GetByTokenAsync(token.Token, CancellationToken.None);

            Assert.True(found.IsSuccess);
            Assert.True(found.Value.IsRevoked);
            Assert.Equal(newTokenValue, found.Value.ReplacedByToken);
        }
        finally
        {
            await unitOfWork.Connection.ExecuteAsync("delete from refresh_token where id = @Id", new { token.Id });
            await unitOfWork.Connection.ExecuteAsync("delete from users where id = @Id", new { user.Id });
        }
    }

    [Fact]
    public async Task GivenMissingToken_WhenQueried_ThenReturnsFailure()
    {
        using var scope = factory.Services.CreateScope();
        var refreshTokenRepository = scope.ServiceProvider.GetRequiredService<IRefreshTokenRepository>();

        var found = await refreshTokenRepository.GetByTokenAsync(
            $"does-not-exist-{Guid.CreateVersion7()}", CancellationToken.None);

        Assert.False(found.IsSuccess);
    }
}
