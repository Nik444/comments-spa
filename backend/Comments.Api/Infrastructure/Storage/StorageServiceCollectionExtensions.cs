using Azure.Identity;
using Azure.Storage.Blobs;
using Comments.Api.Application.Abstractions;
using Comments.Api.Application.Options;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace Comments.Api.Infrastructure.Storage;

public static class StorageServiceCollectionExtensions
{
    public static IServiceCollection AddFileStorage(this IServiceCollection services)
    {
        services.AddOptions<StorageOptions>()
            .BindConfiguration(StorageOptions.SectionName)
            .ValidateDataAnnotations()
            .Validate(
                options => options.Provider != StorageProvider.AzureBlob
                    || !string.IsNullOrWhiteSpace(options.ConnectionString)
                    || Uri.TryCreate(options.ServiceUri, UriKind.Absolute, out _),
                "Для Storage:Provider = AzureBlob нужна Storage:ConnectionString или адрес Storage:ServiceUri.")
            .ValidateOnStart();

        services.AddSingleton<IFileStorage>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<StorageOptions>>().Value;
            var storage = CreateStorage(provider, options);

            return options.CacheMinutes > 0
                ? new CachedFileStorage(storage, provider.GetRequiredService<HybridCache>(), TimeSpan.FromMinutes(options.CacheMinutes))
                : storage;
        });

        return services;
    }

    // Выбор хранилища: остальной код видит только IFileStorage.
    private static IFileStorage CreateStorage(IServiceProvider provider, StorageOptions options)
    {
        if (options.Provider == StorageProvider.AzureBlob)
            return new AzureBlobFileStorage(CreateContainerClient(options));

        var contentRoot = provider.GetRequiredService<IHostEnvironment>().ContentRootPath;

        return new LocalFileStorage(Path.GetFullPath(options.Path, contentRoot));
    }

    // Строка подключения (ключ аккаунта) или, если ключи на аккаунте запрещены, вход без секретов через Entra ID.
    private static BlobContainerClient CreateContainerClient(StorageOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.ConnectionString))
            return new BlobContainerClient(options.ConnectionString, options.Container);

        var containerUri = new Uri($"{options.ServiceUri!.TrimEnd('/')}/{options.Container}");

        return new BlobContainerClient(containerUri, new DefaultAzureCredential());
    }
}
