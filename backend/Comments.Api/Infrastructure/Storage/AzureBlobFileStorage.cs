using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Comments.Api.Application.Abstractions;
using Comments.Api.Domain;

namespace Comments.Api.Infrastructure.Storage;

// Имя блоба равно ключу вложения. Файлы неизменяемы, поэтому перезапись и временные файлы не нужны:
// блоб становится виден читателям только после завершения загрузки.
public sealed class AzureBlobFileStorage(BlobContainerClient container) : IFileStorage
{
    public async Task SaveAsync(string key, byte[] content, CancellationToken cancellationToken)
    {
        var options = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = AttachmentKeys.ContentType(key) }
        };

        await BlobOf(key).UploadAsync(BinaryData.FromBytes(content), options, cancellationToken);
    }

    public async Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            return await BlobOf(key).OpenReadAsync(cancellationToken: cancellationToken);
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return null;
        }
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken) =>
        await BlobOf(key).DeleteIfExistsAsync(cancellationToken: cancellationToken);

    private BlobClient BlobOf(string key)
    {
        if (!AttachmentKeys.IsValid(key))
            throw new ArgumentException("Недопустимый ключ файла.", nameof(key));

        return container.GetBlobClient(key);
    }
}
