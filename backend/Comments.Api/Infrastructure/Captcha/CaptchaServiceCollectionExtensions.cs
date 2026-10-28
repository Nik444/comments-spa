using Comments.Api.Application.Abstractions;
using Comments.Api.Application.Services;

namespace Comments.Api.Infrastructure.Captcha;

public static class CaptchaServiceCollectionExtensions
{
    public static IServiceCollection AddCaptcha(this IServiceCollection services)
    {
        services.AddSingleton<ICaptchaStore, MemoryCaptchaStore>();
        services.AddSingleton<ICaptchaImageRenderer, SkiaCaptchaImageRenderer>();
        services.AddSingleton<ICaptchaService, CaptchaService>();

        return services;
    }
}
