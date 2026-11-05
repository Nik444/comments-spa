using Comments.Api.Application.Abstractions;
using Comments.Api.Application.Options;
using Comments.Api.Application.Services;
using Comments.Api.Infrastructure.Captcha;
using Microsoft.Extensions.Options;

namespace Comments.Tests.Application.Services;

public class CaptchaServiceTests
{
    private readonly FakeRenderer renderer = new();

    private CaptchaService CreateService(bool enabled = true) =>
        new(Options.Create(new CaptchaOptions { Enabled = enabled, CodeLength = 5 }), new MemoryCaptchaStore(Options.Create(new CaptchaOptions())), renderer);

    [Fact]
    public async Task CreateAsync_ReturnsIdAndPngDataUrl()
    {
        var response = await CreateService().CreateAsync(CancellationToken.None);

        Assert.NotEmpty(response.Id);
        Assert.StartsWith("data:image/png;base64,", response.Image);
    }

    [Fact]
    public async Task CreateAsync_UsesCodeOfConfiguredLengthWithoutAmbiguousSymbols()
    {
        await CreateService().CreateAsync(CancellationToken.None);

        Assert.Equal(5, renderer.LastCode!.Length);
        Assert.DoesNotContain(renderer.LastCode, c => "01ILO".Contains(c));
    }

    [Fact]
    public async Task VerifyAsync_ReturnsTrue_ForCorrectAnswer()
    {
        var service = CreateService();
        var captcha = await service.CreateAsync(CancellationToken.None);

        Assert.True(await service.VerifyAsync(captcha.Id, renderer.LastCode, CancellationToken.None));
    }

    [Fact]
    public async Task VerifyAsync_IgnoresCaseAndSpaces()
    {
        var service = CreateService();
        var captcha = await service.CreateAsync(CancellationToken.None);

        Assert.True(await service.VerifyAsync(captcha.Id, $" {renderer.LastCode!.ToLowerInvariant()} ", CancellationToken.None));
    }

    [Fact]
    public async Task VerifyAsync_ReturnsFalse_ForWrongAnswer()
    {
        var service = CreateService();
        var captcha = await service.CreateAsync(CancellationToken.None);

        Assert.False(await service.VerifyAsync(captcha.Id, "WRONG", CancellationToken.None));
    }

    [Fact]
    public async Task VerifyAsync_BurnsCaptchaAfterWrongAnswer()
    {
        var service = CreateService();
        var captcha = await service.CreateAsync(CancellationToken.None);

        await service.VerifyAsync(captcha.Id, "WRONG", CancellationToken.None);

        Assert.False(await service.VerifyAsync(captcha.Id, renderer.LastCode, CancellationToken.None));
    }

    [Fact]
    public async Task VerifyAsync_AcceptsCaptchaOnlyOnce()
    {
        var service = CreateService();
        var captcha = await service.CreateAsync(CancellationToken.None);

        await service.VerifyAsync(captcha.Id, renderer.LastCode, CancellationToken.None);

        Assert.False(await service.VerifyAsync(captcha.Id, renderer.LastCode, CancellationToken.None));
    }

    [Theory]
    [InlineData(null, "ABCDE")]
    [InlineData("", "ABCDE")]
    [InlineData("unknown", "ABCDE")]
    [InlineData("unknown", null)]
    [InlineData("unknown", "")]
    public async Task VerifyAsync_ReturnsFalse_ForMissingOrUnknownData(string? id, string? answer)
    {
        Assert.False(await CreateService().VerifyAsync(id, answer, CancellationToken.None));
    }

    [Fact]
    public async Task VerifyAsync_ReturnsTrue_WhenCaptchaIsDisabled()
    {
        Assert.True(await CreateService(enabled: false).VerifyAsync(null, null, CancellationToken.None));
    }

    private sealed class FakeRenderer : ICaptchaImageRenderer
    {
        public string? LastCode { get; private set; }

        public byte[] Render(string code)
        {
            LastCode = code;

            return [1, 2, 3];
        }
    }
}
