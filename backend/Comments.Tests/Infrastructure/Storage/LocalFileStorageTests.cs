using Comments.Api.Domain;
using Comments.Api.Infrastructure.Storage;

namespace Comments.Tests.Infrastructure.Storage;

public sealed class LocalFileStorageTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "comments-tests-" + Guid.NewGuid().ToString("N"));

    private LocalFileStorage Storage => new(root);

    public void Dispose()
    {
        if (Directory.Exists(root))
            Directory.Delete(root, recursive: true);
    }

    [Fact]
    public async Task SaveAsync_ThenOpenRead_ReturnsSameBytes()
    {
        var key = AttachmentKeys.Create(".txt");
        byte[] content = [1, 2, 3, 4, 5];

        await Storage.SaveAsync(key, content, CancellationToken.None);

        await using var stream = await Storage.OpenReadAsync(key, CancellationToken.None);
        Assert.NotNull(stream);

        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory);

        Assert.Equal(content, memory.ToArray());
    }

    [Fact]
    public async Task SaveAsync_PutsFileIntoSubfolderByKeyPrefix()
    {
        var key = AttachmentKeys.Create(".png");

        await Storage.SaveAsync(key, [1], CancellationToken.None);

        Assert.True(File.Exists(Path.Combine(root, key[..2], key)));
    }

    [Fact]
    public async Task SaveAsync_LeavesNoTemporaryFiles()
    {
        await Storage.SaveAsync(AttachmentKeys.Create(".gif"), [1, 2], CancellationToken.None);

        Assert.Empty(Directory.GetFiles(root, "*.tmp", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task OpenReadAsync_ReturnsNull_WhenFileMissing()
    {
        Assert.Null(await Storage.OpenReadAsync(AttachmentKeys.Create(".png"), CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAsync_RemovesFile_AndIsSafeToRepeat()
    {
        var key = AttachmentKeys.Create(".txt");
        await Storage.SaveAsync(key, [1], CancellationToken.None);

        await Storage.DeleteAsync(key, CancellationToken.None);
        await Storage.DeleteAsync(key, CancellationToken.None);

        Assert.Null(await Storage.OpenReadAsync(key, CancellationToken.None));
    }

    [Theory]
    [InlineData("../evil.txt")]
    [InlineData("..\\evil.txt")]
    [InlineData("not-a-key")]
    public async Task AllOperations_RejectInvalidKeys(string key)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => Storage.SaveAsync(key, [1], CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => Storage.OpenReadAsync(key, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => Storage.DeleteAsync(key, CancellationToken.None));
    }
}
