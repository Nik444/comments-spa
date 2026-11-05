namespace Comments.Api.Application.Abstractions;

public interface IFileStorage
{
    Task SaveAsync(string key, byte[] content, CancellationToken cancellationToken);

    /// <summary>Null, если файла нет.</summary>
    Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken);

    Task DeleteAsync(string key, CancellationToken cancellationToken);
}
