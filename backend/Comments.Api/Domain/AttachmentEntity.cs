namespace Comments.Api.Domain;

public class AttachmentEntity
{
    public required AttachmentKind Kind { get; set; }

    /// <summary>Ключ в хранилище, генерируется сервером. Не URL и не путь.</summary>
    public required string Key { get; set; }

    /// <summary>Имя файла от пользователя, только для показа. Сервис очищает его от путей.</summary>
    public required string OriginalName { get; set; }

    public long SizeBytes { get; set; }
}
