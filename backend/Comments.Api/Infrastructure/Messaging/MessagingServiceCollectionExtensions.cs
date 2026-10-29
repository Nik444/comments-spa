using Comments.Api.Application.Abstractions;
using Comments.Api.Application.Options;

namespace Comments.Api.Infrastructure.Messaging;

public static class MessagingServiceCollectionExtensions
{
    public static IServiceCollection AddMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RabbitMqOptions>()
            .BindConfiguration(RabbitMqOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var enabled = configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>()?.Enabled ?? true;

        if (!enabled)
        {
            services.AddSingleton<ICommentEventPublisher, NullCommentEventPublisher>();
            return services;
        }

        // Один и тот же объект работает и как фоновая задача, и как издатель событий.
        services.AddSingleton<RabbitMqCommentEventPublisher>();
        services.AddSingleton<ICommentEventPublisher>(sp => sp.GetRequiredService<RabbitMqCommentEventPublisher>());
        services.AddHostedService(sp => sp.GetRequiredService<RabbitMqCommentEventPublisher>());

        return services;
    }
}
