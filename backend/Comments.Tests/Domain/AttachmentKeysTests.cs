using Comments.Api.Domain;

namespace Comments.Tests.Domain;

public class AttachmentKeysTests
{
    [Theory]
    [InlineData(".png")]
    [InlineData(".jpg")]
    [InlineData(".gif")]
    [InlineData(".txt")]
    public void Create_ReturnsValidKey(string extension)
    {
        var key = AttachmentKeys.Create(extension);

        Assert.True(AttachmentKeys.IsValid(key));
        Assert.EndsWith(extension, key);
    }

    [Fact]
    public void Create_ReturnsDifferentKeys()
    {
        Assert.NotEqual(AttachmentKeys.Create(".png"), AttachmentKeys.Create(".png"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("../../etc/passwd")]
    [InlineData("0123456789abcdef0123456789abcdef.exe")]
    [InlineData("0123456789abcdef0123456789abcdef.PNG")]
    [InlineData("0123456789ABCDEF0123456789abcdef.png")]
    [InlineData("0123456789abcdef0123456789abcde.png")]
    [InlineData("0123456789abcdef0123456789abcdefg.png")]
    [InlineData("..\\0123456789abcdef0123456789abcdef.png")]
    [InlineData("0123456789abcdef0123456789abcdef.png/")]
    public void IsValid_RejectsUnsafeKeys(string? key)
    {
        Assert.False(AttachmentKeys.IsValid(key));
    }

    [Theory]
    [InlineData("0123456789abcdef0123456789abcdef.png", "image/png")]
    [InlineData("0123456789abcdef0123456789abcdef.jpg", "image/jpeg")]
    [InlineData("0123456789abcdef0123456789abcdef.gif", "image/gif")]
    [InlineData("0123456789abcdef0123456789abcdef.txt", "text/plain; charset=utf-8")]
    public void ContentType_MatchesExtension(string key, string expected)
    {
        Assert.Equal(expected, AttachmentKeys.ContentType(key));
    }

    [Fact]
    public void Url_PointsToAttachmentsRoute()
    {
        Assert.Equal("/api/attachments/abc.png", AttachmentKeys.Url("abc.png"));
    }
}
