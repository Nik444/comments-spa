using System.Text.Json;
using Comments.Api.Application.Abstractions;
using Comments.Api.Application.Options;
using Comments.Api.Application.Responses;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Comments.Api.Infrastructure.Caching;

// HybridCache не умеет читать пачку ключей, поэтому два уровня (память и Redis) собраны здесь вручную.
// Ошибки Redis не ломают запрос: комментарии просто читаются из базы.
public sealed class CommentByIdCache(
    IOptions<CacheOptions> options,
    ILogger<CommentByIdCache> logger,
    IDistributedCache? distributed = null) : ICommentByIdCache, IDisposable
{
    private readonly CacheOptions settings = options.Value;

    private readonly MemoryCache memory = new(new MemoryCacheOptions { SizeLimit = options.Value.CommentLocalMaxItems });

    private IDistributedCache? Redis => settings.Mode == CacheMode.Hybrid ? distributed : null;

    // Возвращает комментарии по списку Id. Главная идея: в базу идёт один запрос на всю пачку, а не по запросу на каждый Id.
    // Пример: просят Id 10, 11, 12. Комментарий 10 лежит в памяти сервера, 11 только в Redis, 12 нигде нет.
    // Тогда 10 берём из памяти, 11 из Redis, и только 12 читаем из базы (если бы таких Id было 100, запрос всё равно один).
    public async Task<IReadOnlyList<CommentResponse>> GetManyAsync(
        IReadOnlyCollection<long> ids,
        Func<IReadOnlyCollection<long>, CancellationToken, Task<IReadOnlyList<CommentResponse>>> loadMissing,
        CancellationToken cancellationToken)
    {
        // Кэш выключен: читаем всё из базы.
        if (settings.Mode == CacheMode.Off)
            return await loadMissing(ids, cancellationToken);

        // found: готовый результат (Id и комментарий), сюда попадает всё найденное, откуда бы оно ни пришло.
        // missing: Id, которые мы пока нигде не нашли.
        var found = new Dictionary<long, CommentResponse>();
        var missing = new List<long>();

        // Шаг 1. Память сервера: самый быстрый уровень, без сети.
        foreach (var id in ids)
        {
            if (memory.TryGetValue(Key(id), out CommentResponse? cached) && cached is not null)
                found[id] = cached;
            else
                missing.Add(id);
        }

        // Шаг 2. Redis (только в режиме Hybrid): ищем то, чего нет в памяти. Все Id запрашиваются параллельно.
        // Найденное кладём и в результат, и в память, чтобы в следующий раз взять его оттуда.
        if (missing.Count > 0 && Redis is not null)
        {
            foreach (var comment in await ReadFromRedisAsync(missing, cancellationToken))
            {
                found[comment.Id] = comment;
                SetInMemory(comment);
            }

            // В списке оставляем только то, чего не оказалось и в Redis.
            missing = missing.Where(id => !found.ContainsKey(id)).ToList();
        }

        // Шаг 3. База: всё, что не нашли нигде, читаем одним запросом (WHERE Id IN (...)).
        // Результат кладём в итог и в кэш (память и Redis), чтобы следующий запрос нашёл эти комментарии уже там.
        if (missing.Count > 0)
        {
            var loaded = await loadMissing(missing, cancellationToken);
            foreach (var comment in loaded)
                found[comment.Id] = comment;

            await StoreAsync(loaded, cancellationToken);
        }

        // Порядок здесь случайный: по Id комментарии сортирует сервис.
        return found.Values.ToList();
    }

    public Task SetAsync(CommentResponse comment, CancellationToken cancellationToken) =>
        settings.Mode == CacheMode.Off ? Task.CompletedTask : StoreAsync([comment], cancellationToken);

    public void Dispose() => memory.Dispose();

    private async Task<List<CommentResponse>> ReadFromRedisAsync(List<long> ids, CancellationToken cancellationToken)
    {
        var redis = Redis!;

        try
        {
            var reads = ids.Select(id => redis.GetAsync(Key(id), cancellationToken)).ToList();
            var results = await Task.WhenAll(reads);

            return results
                .Where(bytes => bytes is not null)
                .Select(bytes => JsonSerializer.Deserialize<CommentResponse>(bytes!)!)
                .ToList();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Не удалось прочитать комментарии из Redis, берём из базы.");
            return [];
        }
    }

    private async Task StoreAsync(IReadOnlyCollection<CommentResponse> comments, CancellationToken cancellationToken)
    {
        foreach (var comment in comments)
            SetInMemory(comment);

        var redis = Redis;
        if (redis is null || comments.Count == 0)
            return;

        var redisOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(settings.CommentRedisMinutes)
        };

        try
        {
            var writes = comments.Select(c => redis.SetAsync(Key(c.Id), JsonSerializer.SerializeToUtf8Bytes(c), redisOptions, cancellationToken));
            await Task.WhenAll(writes);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Не удалось записать комментарии в Redis.");
        }
    }

    private void SetInMemory(CommentResponse comment) =>
        memory.Set(Key(comment.Id), comment, new MemoryCacheEntryOptions
        {
            Size = 1,
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(settings.CommentLocalMinutes)
        });

    private static string Key(long id) => $"comment:{id}";
}
