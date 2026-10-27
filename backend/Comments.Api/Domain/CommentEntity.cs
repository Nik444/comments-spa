namespace Comments.Api.Domain;

public class CommentEntity
{
    public long Id { get; set; }

    /// <summary>Комментарий, на который отвечают. Null у корневого.</summary>
    public long? ParentId { get; set; }

    /// <summary>Корень ветки, null у корневого. Позволяет читать всю ветку одним запросом без рекурсии.</summary>
    public long? RootId { get; set; }

    public required string UserName { get; set; }
    public required string Email { get; set; }
    public string? HomePage { get; set; }
    public required string Text { get; set; }

    /// <summary>Всегда UTC.</summary>
    public DateTime CreatedAt { get; set; }

    public AttachmentEntity? Attachment { get; set; }
}
