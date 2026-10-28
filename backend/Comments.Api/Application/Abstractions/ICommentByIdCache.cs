using Comments.Api.Application.Responses;

namespace Comments.Api.Application.Abstractions;

public interface ICommentByIdCache
{
    // Находит комментарии в кэше, а недостающие одним вызовом loadMissing читает из базы и кладёт в кэш.
    Task<IReadOnlyList<CommentResponse>> GetManyAsync(
        IReadOnlyCollection<long> ids,
        Func<IReadOnlyCollection<long>, CancellationToken, Task<IReadOnlyList<CommentResponse>>> loadMissing,
        CancellationToken cancellationToken);

    Task SetAsync(CommentResponse comment, CancellationToken cancellationToken);
}
