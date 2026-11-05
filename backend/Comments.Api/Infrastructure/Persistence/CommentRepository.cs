using Comments.Api.Application.Abstractions;
using Comments.Api.Application.Models;
using Comments.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Comments.Api.Infrastructure.Persistence;

public class CommentRepository(AppDbContext db) : ICommentRepository
{
    public Task<ParentRef?> GetParentAsync(long id, CancellationToken cancellationToken) =>
        db.Comments
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new ParentRef(c.Id, c.RootId))
            .FirstOrDefaultAsync(cancellationToken);

    public Task<int> CountRepliesAsync(long rootId, CancellationToken cancellationToken) =>
        db.Comments.CountAsync(c => c.RootId == rootId, cancellationToken);

    public async Task<IReadOnlyList<CommentEntity>> GetRootsPageAsync(
        CommentSortField sortBy,
        SortDirection direction,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        var roots = db.Comments.AsNoTracking().Where(c => c.ParentId == null);

        // Id вторым ключом делает порядок однозначным; направление совпадает с индексами таблицы.
        var ordered = (sortBy, direction) switch
        {
            (CommentSortField.Date, SortDirection.Asc) => roots.OrderBy(c => c.CreatedAt).ThenBy(c => c.Id),
            (CommentSortField.Date, SortDirection.Desc) => roots.OrderByDescending(c => c.CreatedAt).ThenByDescending(c => c.Id),
            (CommentSortField.UserName, SortDirection.Asc) => roots.OrderBy(c => c.UserName).ThenBy(c => c.Id),
            (CommentSortField.UserName, SortDirection.Desc) => roots.OrderByDescending(c => c.UserName).ThenByDescending(c => c.Id),
            (CommentSortField.Email, SortDirection.Asc) => roots.OrderBy(c => c.Email).ThenBy(c => c.Id),
            (CommentSortField.Email, SortDirection.Desc) => roots.OrderByDescending(c => c.Email).ThenByDescending(c => c.Id),
            _ => throw new ArgumentOutOfRangeException(nameof(sortBy), sortBy, "Неизвестная сортировка.")
        };

        // Сначала только Id страницы: такой запрос читает один индекс и не поднимает строки пропущенных страниц.
        var ids = await ordered.Skip(skip).Take(take).Select(c => c.Id).ToListAsync(cancellationToken);
        if (ids.Count == 0)
            return [];

        var rows = await db.Comments.AsNoTracking().Where(c => ids.Contains(c.Id)).ToDictionaryAsync(c => c.Id, cancellationToken);
        return ids.Where(rows.ContainsKey).Select(id => rows[id]).ToList();
    }

    public Task<long> CountRootsAsync(CancellationToken cancellationToken) =>
        db.Comments.LongCountAsync(c => c.ParentId == null, cancellationToken);

    public async Task<IReadOnlyList<CommentEntity>> GetRepliesAsync(IReadOnlyCollection<long> rootIds, CancellationToken cancellationToken) =>
        await db.Comments
            .AsNoTracking()
            .Where(c => rootIds.Contains(c.RootId!.Value))
            .OrderBy(c => c.RootId).ThenBy(c => c.CreatedAt).ThenBy(c => c.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CommentEntity>> GetByIdsAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken) =>
        await db.Comments
            .AsNoTracking()
            .Where(c => ids.Contains(c.Id))
            .ToListAsync(cancellationToken);

    public void Add(CommentEntity comment) => db.Comments.Add(comment);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
