namespace Comments.Api.Domain;

/// <summary>
/// Комментарий. Корневой (заглавный) не имеет родителя; ответ хранит ссылку на родителя (ParentId)
/// и на корень своей ветки (RootId), чтобы вся ветка читалась одним запросом без рекурсии.
/// </summary>
public class CommentEntity
{
    public long Id { get; set; }

    /// <summary>Комментарий, на который отвечают. Null у корневого.</summary>
    public long? ParentId { get; set; }

    /// <summary>Корень ветки. Null у корневого. Проставляется сервисом при создании ответа.</summary>
    public long? RootId { get; set; }

    public required string UserName { get; set; }
    public required string Email { get; set; }
    public string? HomePage { get; set; }

    /// <summary>Текст с разрешёнными тегами, уже проверенный сервисом.</summary>
    public required string Text { get; set; }

    /// <summary>Время создания, всегда UTC.</summary>
    public DateTime CreatedAt { get; set; }

    public AttachmentEntity? Attachment { get; set; }
}
