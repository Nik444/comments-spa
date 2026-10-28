using Comments.Api.Application.Abstractions;
using Comments.Api.Application.Options;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace Comments.Api.Infrastructure.Caching;

public sealed class PageCache(HybridCache cache, IOptions<CacheOptions> options) : IPageCache
{
    private readonly CacheOptions settings = options.Value;

    private readonly HybridCacheEntryOptions entryOptions = new()
    {
        LocalCacheExpiration = TimeSpan.FromSeconds(options.Value.PageLocalSeconds),
        Expiration = TimeSpan.FromSeconds(options.Value.PageRedisSeconds)
    };

    public ValueTask<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, ValueTask<T>> factory, CancellationToken cancellationToken) =>
        settings.Mode == CacheMode.Off
            ? factory(cancellationToken)
            : cache.GetOrCreateAsync(key, factory, entryOptions, cancellationToken: cancellationToken);

    public bool IsCacheablePage(int page, int totalPages) =>
        settings.Mode != CacheMode.Off
        && page <= totalPages
        && (page <= settings.HeadPages || page > totalPages - settings.TailPages);
}
