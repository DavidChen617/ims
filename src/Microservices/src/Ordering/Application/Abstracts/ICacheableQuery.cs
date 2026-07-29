namespace Application.Abstracts;

public interface ICacheableQuery
{
    string CacheKey { get; }
    TimeSpan CacheTtl { get; }
}
