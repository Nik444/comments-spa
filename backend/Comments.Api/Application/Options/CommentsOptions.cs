using System.ComponentModel.DataAnnotations;

namespace Comments.Api.Application.Options;

public sealed class CommentsOptions
{
    public const string SectionName = "Comments";

    /// <summary>Сколько корневых комментариев на странице.</summary>
    [Range(1, 100)]
    public int PageSize { get; set; } = 25;

    /// <summary>Максимум ответов в одной ветке. Защита от злоупотреблений, в ТЗ такого ограничения нет.</summary>
    [Range(1, 100_000)]
    public int MaxRepliesPerThread { get; set; } = 1000;

    /// <summary>Максимум Id в одном запросе на подгрузку комментариев.</summary>
    [Range(1, 1000)]
    public int MaxIdsPerRequest { get; set; } = 200;
}
