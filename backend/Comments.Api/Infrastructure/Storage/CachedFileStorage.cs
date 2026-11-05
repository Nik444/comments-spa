using Comments.Api.Application.Abstractions;
using Microsoft.Extensions.Caching.Hybrid;

namespace Comments.Api.Infrastructure.Storage;

// Кэш перед любым хранилищем. Вложения неизменяемы (ключ уникален), поэтому сбрасывать кэш нужно только при удалении.
// При Cache:Mode = Hybrid значения лежат и в Redis: их видят все узлы Api, а облако читается один раз.
// Размер ограничен правилами проверки (картинка до 1 МБ, текст до 100 КБ).
public sealed class CachedFileStorage(IFileStorage inner, HybridCache cache, TimeSpan ttl) : IFileStorage
{
    private static readonly TimeSpan MaxLocalTtl = TimeSpan.FromMinutes(5);

    private readonly HybridCacheEntryOptions entryOptions = new()
    {
        Expiration = ttl,
        LocalCacheExpiration = ttl < MaxLocalTtl ? ttl : MaxLocalTtl
    };

    // Сразу после загрузки файл обычно открывают: кладём его в кэш вместе с записью в хранилище.
    public async Task SaveAsync(string key, byte[] content, CancellationToken cancellationToken)
    {
        await inner.SaveAsync(key, content, cancellationToken);
        await cache.SetAsync(CacheKey(key), content, entryOptions, cancellationToken: cancellationToken);
    }

    public async Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            // Исключение из фабрики HybridCache не кэширует, поэтому отсутствующий файл не «залипает».
            var bytes = await cache.GetOrCreateAsync(
                CacheKey(key),
                async token =>
                {
                    await using var stream = await inner.OpenReadAsync(key, token) ?? throw new FileNotFoundException(key);
                    using var buffer = new MemoryStream();
                    await stream.CopyToAsync(buffer, token);
                    return buffer.ToArray();
                },
                entryOptions,
                cancellationToken: cancellationToken);

            return new MemoryStream(bytes, writable: false);
        }
        catch (FileNotFoundException)
        {
            return null;
        }
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        await inner.DeleteAsync(key, cancellationToken);
        await cache.RemoveAsync(CacheKey(key), cancellationToken);
    }

    private static string CacheKey(string key) => $"file:{key}";
}
