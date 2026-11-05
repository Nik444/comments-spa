using System.ComponentModel.DataAnnotations;

namespace Comments.Api.Application.Options;

public enum StorageProvider
{
    Local,
    AzureBlob
}

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public StorageProvider Provider { get; set; } = StorageProvider.Local;

    /// <summary>Папка с файлами (Provider = Local). Относительный путь считается от папки проекта. В Docker это том.</summary>
    [Required]
    public string Path { get; set; } = "data/attachments";

    /// <summary>Строка подключения к Azure Storage (Provider = AzureBlob). В репозиторий не кладём: только переменные окружения или user-secrets.</summary>
    public string? ConnectionString { get; set; }

    /// <summary>Контейнер Blob-хранилища. Должен существовать заранее.</summary>
    public string Container { get; set; } = "uploads";

    /// <summary>
    /// Сколько минут держать файлы в кэше перед хранилищем (0 = кэш выключен). Имеет смысл для облака.
    /// При Cache:Mode = Hybrid кэш лежит ещё и в Redis.
    /// </summary>
    [Range(0, 24 * 60)]
    public int CacheMinutes { get; set; }
}
