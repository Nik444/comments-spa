using Comments.Api.Application.Responses;
using Comments.Api.Domain;

namespace Comments.Api.Application.Mapping;

public static class CommentMappings
{
    public static CommentResponse ToResponse(this CommentEntity comment) => new(
        comment.Id,
        comment.ParentId,
        comment.RootId,
        comment.UserName,
        comment.Email,
        comment.HomePage,
        comment.Text,
        AsUtc(comment.CreatedAt),
        ToResponse(comment.Attachment));

    public static RootCommentResponse ToRootResponse(this CommentEntity comment, IReadOnlyList<CommentResponse> replies) => new(
        comment.Id,
        comment.UserName,
        comment.Email,
        comment.HomePage,
        comment.Text,
        AsUtc(comment.CreatedAt),
        ToResponse(comment.Attachment),
        replies);

    private static AttachmentResponse? ToResponse(AttachmentEntity? attachment) => attachment is null
        ? null
        : new AttachmentResponse(attachment.Kind.ToString().ToLowerInvariant(), AttachmentKeys.Url(attachment.Key), attachment.OriginalName, attachment.SizeBytes);

    // База хранит UTC, но при чтении EF не знает об этом. Помечаем явно, чтобы в JSON было "...Z".
    private static DateTime AsUtc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
