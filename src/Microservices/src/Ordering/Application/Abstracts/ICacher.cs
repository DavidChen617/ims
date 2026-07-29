namespace Application.Abstracts;

public interface ICacher
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct);
    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct);
    Task DeleteByPrefixAsync(string prefix, CancellationToken ct);
}
