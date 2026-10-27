using Comments.Api.Application.Models;
using Comments.Api.Domain;

namespace Comments.Api.Application.Abstractions;

public interface ICommentRepository
{
    Task<ParentRef?> GetParentAsync(long id, CancellationToken cancellationToken);

    Task<int> CountRepliesAsync(long rootId, CancellationToken cancellationToken);

    Task<IReadOnlyList<CommentEntity>> GetRootsPageAsync(
        CommentSortField sortBy,
        SortDirection direction,
        int skip,
        int take,
        CancellationToken cancellationToken);

    Task<long> CountRootsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<CommentEntity>> GetRepliesAsync(IReadOnlyCollection<long> rootIds, CancellationToken cancellationToken);

    Task<IReadOnlyList<CommentEntity>> GetByIdsAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken);

    // В базу попадёт после SaveChangesAsync.
    void Add(CommentEntity comment);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
