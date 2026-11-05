using Comments.Api.Application.Mapping;
using Comments.Api.Domain;

namespace Comments.Tests.Application.Mapping;

public class CommentMappingsTests
{
    [Fact]
    public void ToResponse_WithoutAttachment_HasNullAttachment()
    {
        Assert.Null(NewComment().ToResponse().Attachment);
    }

    [Theory]
    [InlineData(AttachmentKind.Image, "image")]
    [InlineData(AttachmentKind.Text, "text")]
    public void ToResponse_MapsAttachment_WithUrlInsteadOfKey(AttachmentKind kind, string expectedKind)
    {
        var key = AttachmentKeys.Create(".png");
        var comment = NewComment();
        comment.Attachment = new AttachmentEntity { Kind = kind, Key = key, OriginalName = "cat.png", SizeBytes = 123 };

        var attachment = comment.ToResponse().Attachment;

        Assert.NotNull(attachment);
        Assert.Equal(expectedKind, attachment.Kind);
        Assert.Equal($"/api/attachments/{key}", attachment.Url);
        Assert.Equal("cat.png", attachment.Name);
        Assert.Equal(123, attachment.Size);
    }

    [Fact]
    public void ToRootResponse_IncludesAttachment()
    {
        var comment = NewComment();
        comment.Attachment = new AttachmentEntity { Kind = AttachmentKind.Text, Key = AttachmentKeys.Create(".txt"), OriginalName = "a.txt", SizeBytes = 5 };

        Assert.Equal("text", comment.ToRootResponse([]).Attachment?.Kind);
    }

    private static CommentEntity NewComment() => new()
    {
        Id = 1,
        UserName = "u",
        Email = "u@example.com",
        Text = "t",
        CreatedAt = new DateTime(2026, 11, 3, 10, 0, 0, DateTimeKind.Utc)
    };
}
