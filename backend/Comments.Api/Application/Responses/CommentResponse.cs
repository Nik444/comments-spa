namespace Comments.Api.Application.Responses;

public sealed record CommentResponse(
    long Id,
    long? ParentId,
    long? RootId,
    string UserName,
    string Email,
    string? HomePage,
    string Text,
    DateTime CreatedAt);
