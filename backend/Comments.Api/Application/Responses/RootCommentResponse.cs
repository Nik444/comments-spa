namespace Comments.Api.Application.Responses;

// Ответы идут плоским списком (у каждого есть ParentId), дерево строит клиент.
public sealed record RootCommentResponse(
    long Id,
    string UserName,
    string Email,
    string? HomePage,
    string Text,
    DateTime CreatedAt,
    IReadOnlyList<CommentResponse> Replies);
