using Application.Abstracts;
using Microsoft.Extensions.DependencyInjection;

namespace Ordering.IntegrationTest;

public class RedisCacheTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private record CachedPayload(string Name, int Count);

    [Fact]
    public async Task GivenValueSet_WhenGet_ThenReturnsSameValue()
    {
        using var scope = factory.Services.CreateScope();
        var cacher = scope.ServiceProvider.GetRequiredService<ICacher>();
        var key = $"test:{Guid.NewGuid()}";
        var value = new CachedPayload("widget", 3);

        await cacher.SetAsync(key, value, TimeSpan.FromMinutes(1), CancellationToken.None);
        var result = await cacher.GetAsync<CachedPayload>(key, CancellationToken.None);

        Assert.Equal(value, result);
    }

    [Fact]
    public async Task GivenNoValue_WhenGet_ThenReturnsDefault()
    {
        using var scope = factory.Services.CreateScope();
        var cacher = scope.ServiceProvider.GetRequiredService<ICacher>();
        var key = $"test:{Guid.NewGuid()}";

        var result = await cacher.GetAsync<CachedPayload>(key, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GivenTtlExpired_WhenGet_ThenReturnsDefault()
    {
        using var scope = factory.Services.CreateScope();
        var cacher = scope.ServiceProvider.GetRequiredService<ICacher>();
        var key = $"test:{Guid.NewGuid()}";
        var value = new CachedPayload("expiring", 1);

        await cacher.SetAsync(key, value, TimeSpan.FromMilliseconds(200), CancellationToken.None);
        await Task.Delay(500);
        var result = await cacher.GetAsync<CachedPayload>(key, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GivenKeysWithPrefix_WhenDeleteByPrefix_ThenOnlyMatchingKeysRemoved()
    {
        using var scope = factory.Services.CreateScope();
        var cacher = scope.ServiceProvider.GetRequiredService<ICacher>();
        var prefix = $"test:prefix:{Guid.NewGuid()}:";
        var matchingKey = $"{prefix}a";
        var otherKey = $"test:other:{Guid.NewGuid()}";

        await cacher.SetAsync(matchingKey, new CachedPayload("match", 1), TimeSpan.FromMinutes(1), CancellationToken.None);
        await cacher.SetAsync(otherKey, new CachedPayload("other", 2), TimeSpan.FromMinutes(1), CancellationToken.None);

        await cacher.DeleteByPrefixAsync(prefix, CancellationToken.None);

        var matchingResult = await cacher.GetAsync<CachedPayload>(matchingKey, CancellationToken.None);
        var otherResult = await cacher.GetAsync<CachedPayload>(otherKey, CancellationToken.None);

        Assert.Null(matchingResult);
        Assert.NotNull(otherResult);
    }
}
