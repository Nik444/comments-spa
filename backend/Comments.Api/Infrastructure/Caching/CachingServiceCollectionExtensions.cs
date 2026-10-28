using Comments.Api.Application.Abstractions;
using Comments.Api.Application.Options;

namespace Comments.Api.Infrastructure.Caching;

public static class CachingServiceCollectionExtensions
{
    public static IServiceCollection AddCommentsCaching(this IServiceCollection services, IConfiguration configuration)
    {
        var mode = configuration.GetSection(CacheOptions.SectionName).Get<CacheOptions>()?.Mode ?? CacheMode.Memory;

        services.AddMemoryCache();

        // Redis подключается только в режиме Hybrid. Без него HybridCache работает на одной памяти.
        if (mode == CacheMode.Hybrid)
        {
            var connection = configuration.GetConnectionString("Redis")
                ?? throw new InvalidOperationException("Не задана строка подключения ConnectionStrings:Redis.");

            services.AddStackExchangeRedisCache(redis =>
            {
                redis.Configuration = connection;
                redis.InstanceName = "comments:";
            });
        }

        // По умолчанию HybridCache не хранит значения больше 1 МБ, а страница с большими ветками может быть тяжелее.
        services.AddHybridCache(hybrid => hybrid.MaximumPayloadBytes = 8 * 1024 * 1024);
        services.AddSingleton<IPageCache, PageCache>();
        services.AddSingleton<ICommentByIdCache, CommentByIdCache>();

        return services;
    }
}
