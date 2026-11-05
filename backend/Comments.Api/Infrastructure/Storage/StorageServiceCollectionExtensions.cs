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
                options => options.Provider != StorageProvider.AzureBlob || !string.IsNullOrWhiteSpace(options.ConnectionString),
                "Для Storage:Provider = AzureBlob нужна строка подключения Storage:ConnectionString.")
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
            return new AzureBlobFileStorage(new BlobContainerClient(options.ConnectionString, options.Container));

        var contentRoot = provider.GetRequiredService<IHostEnvironment>().ContentRootPath;

        return new LocalFileStorage(Path.GetFullPath(options.Path, contentRoot));
    }
}
