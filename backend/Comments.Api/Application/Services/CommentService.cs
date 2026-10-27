using Comments.Api.Application.Abstractions;
using Comments.Api.Application.Exceptions;
using Comments.Api.Application.Mapping;
using Comments.Api.Application.Options;
using Comments.Api.Application.Requests;
using Comments.Api.Application.Responses;
using Comments.Api.Domain;
using Microsoft.Extensions.Options;

namespace Comments.Api.Application.Services;

public class CommentService(ICommentRepository repository, IOptions<CommentsOptions> options, TimeProvider timeProvider)
{
    public async Task<CommentResponse> CreateAsync(CreateCommentRequest request, CancellationToken cancellationToken)
    {
        long? rootId = null;

        if (request.ParentId is { } parentId)
        {
            var parent = await repository.GetParentAsync(parentId, cancellationToken)
                ?? throw new ParentNotFoundException(parentId);

            // Корень ветки: у ответа это RootId родителя, а если родитель сам корневой, то его Id.
            rootId = parent.RootId ?? parent.Id;

            var limit = options.Value.MaxRepliesPerThread;
            if (await repository.CountRepliesAsync(rootId.Value, cancellationToken) >= limit)
            {
                throw new ReplyLimitExceededException(rootId.Value, limit);
            }
        }

        var comment = new CommentEntity
        {
            ParentId = request.ParentId,
            RootId = rootId,
            UserName = request.UserName.Trim(),
            Email = request.Email.Trim(),
            HomePage = string.IsNullOrWhiteSpace(request.HomePage) ? null : request.HomePage.Trim(),
            Text = request.Text,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime
        };

        repository.Add(comment);
        await repository.SaveChangesAsync(cancellationToken);

        return comment.ToResponse();
    }

    public async Task<PagedResponse<RootCommentResponse>> GetPageAsync(GetCommentsRequest request, CancellationToken cancellationToken)
    {
        var pageSize = options.Value.PageSize;
        var totalItems = await repository.CountRootsAsync(cancellationToken);
        var totalPages = (int)((totalItems + pageSize - 1) / pageSize);

        var roots = await repository.GetRootsPageAsync(
            request.SortBy,
            request.SortDir,
            (request.Page - 1) * pageSize,
            pageSize,
            cancellationToken);

        if (roots.Count == 0)
            return new PagedResponse<RootCommentResponse>([], request.Page, pageSize, totalItems, totalPages);

        // Второй запрос: ответы сразу всех корней страницы, дерево собирает клиент.
        var replies = await repository.GetRepliesAsync(roots.Select(r => r.Id).ToList(), cancellationToken);
        var repliesByRoot = replies.ToLookup(r => r.RootId!.Value, r => r.ToResponse());

        var items = roots
            .Select(root => root.ToRootResponse(repliesByRoot[root.Id].ToList()))
            .ToList();

        return new PagedResponse<RootCommentResponse>(items, request.Page, pageSize, totalItems, totalPages);
    }

    // Порядок по возрастанию Id, несуществующие Id пропускаются.
    public async Task<IReadOnlyList<CommentResponse>> GetByIdsAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken)
    {
        var distinctIds = ids.Distinct().ToList();

        var limit = options.Value.MaxIdsPerRequest;
        if (distinctIds.Count > limit)
            throw new TooManyIdsException(limit);

        if (distinctIds.Count == 0)
            return [];

        var comments = await repository.GetByIdsAsync(distinctIds, cancellationToken);
        return comments.OrderBy(c => c.Id).Select(c => c.ToResponse()).ToList();
    }

    public async Task<CommentResponse?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        var comments = await repository.GetByIdsAsync([id], cancellationToken);
        return comments.Count == 0 ? null : comments[0].ToResponse();
    }
}
