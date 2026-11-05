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

        // [разбор] Если RabbitMq:Enabled выключен, подставляем заглушку, которая ничего не делает. CommentService о ней ничего не знает.
        if (!enabled)
        {
            services.AddSingleton<ICommentEventPublisher, NullCommentEventPublisher>();
            return services;
        }

        // [разбор] Нужен один объект на двоих: и фоновая задача (отправляет), и издатель (принимает события от CommentService).
        // Один и тот же объект работает и как фоновая задача, и как издатель событий.
        services.AddSingleton<RabbitMqCommentEventPublisher>();
        services.AddSingleton<ICommentEventPublisher>(sp => sp.GetRequiredService<RabbitMqCommentEventPublisher>());
        services.AddHostedService(sp => sp.GetRequiredService<RabbitMqCommentEventPublisher>());

        return services;
    }
}
