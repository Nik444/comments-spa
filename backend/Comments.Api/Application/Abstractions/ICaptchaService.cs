using Comments.Api.Application.Responses;

namespace Comments.Api.Application.Abstractions;

public interface ICaptchaService
{
    bool IsEnabled { get; }

    Task<CaptchaResponse> CreateAsync(CancellationToken cancellationToken);

    // Каждая капча проверяется один раз, неверный ответ тоже её "сжигает". Если капча выключена, всегда true.
    Task<bool> VerifyAsync(string? id, string? answer, CancellationToken cancellationToken);
}
