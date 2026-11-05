using System.ComponentModel.DataAnnotations;

namespace Comments.Realtime.Options;

public sealed class RealtimeOptions
{
    public const string SectionName = "Realtime";

    /// <summary>Как часто накопленные события уходят клиентам одной пачкой.</summary>
    [Range(100, 10_000)]
    public int BatchIntervalMilliseconds { get; set; } = 1000;

    /// <summary>На сколько корневых комментариев может подписаться одно подключение. Одна страница показывает 25.</summary>
    [Range(1, 500)]
    public int MaxRootsPerSubscription { get; set; } = 50;

    /// <summary>Сколько событий ждёт отправки в памяти. При переполнении самые старые отбрасываются.</summary>
    [Range(100, 10_000_000)]
    public int BufferCapacity { get; set; } = 100_000;

    /// <summary>Сайты, которым разрешено подключаться. Пусто значит любые (удобно для разработки).</summary>
    public string[] AllowedOrigins { get; set; } = [];
}
