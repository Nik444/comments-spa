using Comments.Api.Application.Abstractions;
using Comments.Api.Domain;
using Comments.Api.Infrastructure.Storage;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;

namespace Comments.Tests.Infrastructure.Storage;

public sealed class CachedFileStorageTests
{
    private readonly FakeStorage inner = new();

    private CachedFileStorage Create()
    {
        var provider = new ServiceCollection().AddHybridCache().Services.BuildServiceProvider();

        return new CachedFileStorage(inner, provider.GetRequiredService<HybridCache>(), TimeSpan.FromMinutes(10));
    }

    private static async Task<byte[]> ReadAllAsync(Stream stream)
    {
        await using (stream)
        {
            using var memory = new MemoryStream();
            await stream.CopyToAsync(memory);

            return memory.ToArray();
        }
    }

    [Fact]
    public async Task OpenRead_SecondRead_DoesNotTouchInnerStorage()
    {
        var storage = Create();
        var key = AttachmentKeys.Create(".txt");
        inner.Files[key] = [1, 2, 3];

        var first = await storage.OpenReadAsync(key, CancellationToken.None);
        var second = await storage.OpenReadAsync(key, CancellationToken.None);

        Assert.Equal(new byte[] { 1, 2, 3 }, await ReadAllAsync(first!));
        Assert.Equal(new byte[] { 1, 2, 3 }, await ReadAllAsync(second!));
        Assert.Equal(1, inner.Opens);
    }

    [Fact]
    public async Task Save_PutsFileIntoInnerAndCache()
    {
        var storage = Create();
        var key = AttachmentKeys.Create(".png");

        await storage.SaveAsync(key, [9, 8], CancellationToken.None);
        var stream = await storage.OpenReadAsync(key, CancellationToken.None);

        Assert.Equal(new byte[] { 9, 8 }, inner.Files[key]);
        Assert.Equal(new byte[] { 9, 8 }, await ReadAllAsync(stream!));
        Assert.Equal(0, inner.Opens);
    }

    [Fact]
    public async Task OpenRead_MissingFile_ReturnsNullAndIsNotCached()
    {
        var storage = Create();
        var key = AttachmentKeys.Create(".txt");

        Assert.Null(await storage.OpenReadAsync(key, CancellationToken.None));

        inner.Files[key] = [7];
        var stream = await storage.OpenReadAsync(key, CancellationToken.None);

        Assert.Equal(new byte[] { 7 }, await ReadAllAsync(stream!));
    }

    [Fact]
    public async Task Delete_RemovesFromInnerAndCache()
    {
        var storage = Create();
        var key = AttachmentKeys.Create(".gif");
        await storage.SaveAsync(key, [1], CancellationToken.None);

        await storage.DeleteAsync(key, CancellationToken.None);

        Assert.Null(await storage.OpenReadAsync(key, CancellationToken.None));
    }

    private sealed class FakeStorage : IFileStorage
    {
        public Dictionary<string, byte[]> Files { get; } = [];

        public int Opens { get; private set; }

        public Task SaveAsync(string key, byte[] content, CancellationToken cancellationToken)
        {
            Files[key] = content;
            return Task.CompletedTask;
        }

        public Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken)
        {
            Opens++;

            return Task.FromResult<Stream?>(Files.TryGetValue(key, out var bytes) ? new MemoryStream(bytes) : null);
        }

        public Task DeleteAsync(string key, CancellationToken cancellationToken)
        {
            Files.Remove(key);
            return Task.CompletedTask;
        }
    }
}
