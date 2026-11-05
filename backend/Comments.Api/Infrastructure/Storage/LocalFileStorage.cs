using Comments.Api.Application.Abstractions;
using Comments.Api.Domain;

namespace Comments.Api.Infrastructure.Storage;

// Файлы лежат в подпапках по первым двум символам ключа, чтобы в одной папке не набирались сотни тысяч файлов.
// На нескольких узлах Api нужен общий том или облачное хранилище: достаточно другой реализации IFileStorage.
public sealed class LocalFileStorage(string root) : IFileStorage
{
    public async Task SaveAsync(string key, byte[] content, CancellationToken cancellationToken)
    {
        var path = PathOf(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        // Сначала временный файл, потом переименование: читатель никогда не увидит недописанный файл.
        var temp = $"{path}.{Guid.NewGuid():N}.tmp";

        try
        {
            await File.WriteAllBytesAsync(temp, content, cancellationToken);
            File.Move(temp, path);
        }
        catch
        {
            File.Delete(temp);
            throw;
        }
    }

    public Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            Stream stream = new FileStream(PathOf(key), FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
            return Task.FromResult<Stream?>(stream);
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            return Task.FromResult<Stream?>(null);
        }
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        File.Delete(PathOf(key));
        return Task.CompletedTask;
    }

    private string PathOf(string key)
    {
        if (!AttachmentKeys.IsValid(key))
            throw new ArgumentException("Недопустимый ключ файла.", nameof(key));

        return Path.Combine(root, key[..2], key);
    }
}
