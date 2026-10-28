using Comments.Api.Application.Abstractions;
using Comments.Api.Application.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Comments.Api.Infrastructure.Captcha;

// Подходит для одного сервера. Для нескольких серверов код должен лежать в общем хранилище (Redis).
public sealed class MemoryCaptchaStore(IOptions<CaptchaOptions> options) : ICaptchaStore, IDisposable
{
    private readonly MemoryCache memory = new(new MemoryCacheOptions { SizeLimit = options.Value.MaxStoredCodes });

    public Task SaveAsync(string id, string code, TimeSpan lifetime, CancellationToken cancellationToken)
    {
        memory.Set(id, new Entry(code), new MemoryCacheEntryOptions { Size = 1, AbsoluteExpirationRelativeToNow = lifetime });

        return Task.CompletedTask;
    }

    public Task<string?> TakeAsync(string id, CancellationToken cancellationToken)
    {
        if (!memory.TryGetValue(id, out Entry? entry))
            return Task.FromResult<string?>(null);

        memory.Remove(id);

        return Task.FromResult(entry!.TryUse());
    }

    public void Dispose() => memory.Dispose();

    // Get и Remove вместе не атомарны: два запроса могут одновременно найти один код.
    // Поэтому код отдаётся только первому, кто его "использует".
    private sealed class Entry(string code)
    {
        private int used;

        public string? TryUse() => Interlocked.Exchange(ref used, 1) == 0 ? code : null;
    }
}
