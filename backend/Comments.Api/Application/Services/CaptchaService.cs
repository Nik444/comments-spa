using System.Security.Cryptography;
using Comments.Api.Application.Abstractions;
using Comments.Api.Application.Options;
using Comments.Api.Application.Responses;
using Microsoft.Extensions.Options;

namespace Comments.Api.Application.Services;

public sealed class CaptchaService(
    IOptions<CaptchaOptions> options,
    ICaptchaStore store,
    ICaptchaImageRenderer renderer) : ICaptchaService
{
    // Латинские буквы и цифры, но без похожих символов (0 и O, 1 и I и L), чтобы человек не гадал.
    private const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    public bool IsEnabled => options.Value.Enabled;

    public async Task<CaptchaResponse> CreateAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;

        // Код и идентификатор берутся из криптографического генератора: их нельзя угадать по предыдущим.
        var code = new string(RandomNumberGenerator.GetItems<char>(Alphabet, settings.CodeLength));
        var id = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));

        await store.SaveAsync(id, code, TimeSpan.FromMinutes(settings.LifetimeMinutes), cancellationToken);

        var image = "data:image/png;base64," + Convert.ToBase64String(renderer.Render(code));

        return new CaptchaResponse(id, image);
    }

    public async Task<bool> VerifyAsync(string? id, string? answer, CancellationToken cancellationToken)
    {
        if (!IsEnabled)
            return true;

        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(answer))
            return false;

        var expected = await store.TakeAsync(id, cancellationToken);

        return expected is not null && string.Equals(expected, answer.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
