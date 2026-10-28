using System.ComponentModel.DataAnnotations;

namespace Comments.Api.Application.Options;

public sealed class CacheOptions
{
    public const string SectionName = "Cache";

    public CacheMode Mode { get; set; } = CacheMode.Memory;

    /// <summary>Сколько первых страниц списка кэшируется. Остальные идут в базу.</summary>
    [Range(0, 100)]
    public int HeadPages { get; set; } = 5;

    /// <summary>Сколько последних страниц списка кэшируется.</summary>
    [Range(0, 100)]
    public int TailPages { get; set; } = 5;

    /// <summary>Срок страницы (и числа страниц) в памяти сервера.</summary>
    [Range(1, 3600)]
    public int PageLocalSeconds { get; set; } = 2;

    /// <summary>Срок страницы (и числа страниц) в Redis, только в режиме Hybrid.</summary>
    [Range(1, 3600)]
    public int PageRedisSeconds { get; set; } = 2;

    /// <summary>Срок комментария по Id в памяти сервера.</summary>
    [Range(1, 1440)]
    public int CommentLocalMinutes { get; set; } = 5;

    /// <summary>Срок комментария по Id в Redis, только в режиме Hybrid.</summary>
    [Range(1, 10080)]
    public int CommentRedisMinutes { get; set; } = 60;

    /// <summary>Сколько комментариев по Id помещается в памяти сервера. Защита от разрастания памяти.</summary>
    [Range(100, 10_000_000)]
    public int CommentLocalMaxItems { get; set; } = 50_000;
}
