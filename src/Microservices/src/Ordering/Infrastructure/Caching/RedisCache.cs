using System.Text.Json;
using Application.Abstracts;
using StackExchange.Redis;

namespace Infrastructure.Caching;

public sealed class RedisCache(IConnectionMultiplexer connectionMultiplexer) : ICacher
{
    public async Task<T?> GetAsync<T>(string key, CancellationToken ct)
    {
        var db = connectionMultiplexer.GetDatabase();
        var value = await db.StringGetAsync(key);

        return value.IsNullOrEmpty ? default : JsonSerializer.Deserialize<T>((string)value!);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct)
    {
        var db = connectionMultiplexer.GetDatabase();
        var json = JsonSerializer.Serialize(value);

        await db.StringSetAsync(key, json, ttl);
    }

    public async Task DeleteByPrefixAsync(string prefix, CancellationToken ct)
    {
        var db = connectionMultiplexer.GetDatabase();
        var server = connectionMultiplexer.GetServer(connectionMultiplexer.GetEndPoints()[0]);
        var keys = server.Keys(db.Database, pattern: $"{prefix}*").ToArray();

        if (keys.Length > 0)
            await db.KeyDeleteAsync(keys);
    }
}
