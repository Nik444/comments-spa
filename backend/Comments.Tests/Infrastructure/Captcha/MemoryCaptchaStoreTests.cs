using Comments.Api.Application.Options;
using Comments.Api.Infrastructure.Captcha;
using Microsoft.Extensions.Options;

namespace Comments.Tests.Infrastructure.Captcha;

public class MemoryCaptchaStoreTests
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);

    private readonly MemoryCaptchaStore store = new(Options.Create(new CaptchaOptions()));

    [Fact]
    public async Task TakeAsync_ReturnsSavedCode()
    {
        await store.SaveAsync("id1", "AB123", Lifetime, CancellationToken.None);

        Assert.Equal("AB123", await store.TakeAsync("id1", CancellationToken.None));
    }

    [Fact]
    public async Task TakeAsync_ReturnsNull_ForUnknownId()
    {
        Assert.Null(await store.TakeAsync("unknown", CancellationToken.None));
    }

    [Fact]
    public async Task TakeAsync_ReturnsCodeOnlyOnce()
    {
        await store.SaveAsync("id1", "AB123", Lifetime, CancellationToken.None);

        await store.TakeAsync("id1", CancellationToken.None);

        Assert.Null(await store.TakeAsync("id1", CancellationToken.None));
    }

    [Fact]
    public async Task TakeAsync_ReturnsCodeToOnlyOneOfParallelCallers()
    {
        await store.SaveAsync("id1", "AB123", Lifetime, CancellationToken.None);

        var results = await Task.WhenAll(
            Enumerable.Range(0, 100).Select(_ => Task.Run(() => store.TakeAsync("id1", CancellationToken.None))));

        Assert.Single(results, code => code is not null);
    }
}
