using Comments.Api.Application.Abstractions;
using Comments.Api.Application.Exceptions;
using Comments.Api.Application.Mapping;
using Comments.Api.Application.Options;
using Comments.Api.Application.Requests;
using Comments.Api.Application.Responses;
using Comments.Api.Domain;
using Comments.Contracts;
using Microsoft.Extensions.Options;

namespace Comments.Api.Application.Services;

public class CommentService(
    ICommentRepository repository,
    ICommentByIdCache commentCache,
    IPageCache pageCache,
    ICaptchaService captcha,
    ICommentEventPublisher events,
    IOptions<CommentsOptions> options,
    TimeProvider timeProvider)
{
    public async Task<CommentResponse> CreateAsync(CreateCommentRequest request, CancellationToken cancellationToken)
    {
        if (!await captcha.VerifyAsync(request.CaptchaId, request.CaptchaAnswer, cancellationToken))
            throw new InvalidCaptchaException();

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

        // В кэш сразу после коммита: клиенты, получившие сигнал о новом комментарии, найдут его там.
        var response = comment.ToResponse();
        await commentCache.SetAsync(response, cancellationToken);

        events.Publish(new CommentCreatedEvent(response.Id, response.ParentId, response.RootId, response.CreatedAt));

        return response;
    }

    public async Task<PagedResponse<RootCommentResponse>> GetPageAsync(GetCommentsRequest request, CancellationToken cancellationToken)
    {
        var totalItems = await pageCache.GetOrCreateAsync("roots:count", ct => new ValueTask<long>(repository.CountRootsAsync(ct)), cancellationToken);

        if (!pageCache.IsCacheablePage(request.Page, TotalPages(totalItems)))
            return await LoadPageAsync(request, totalItems, cancellationToken);

        var key = $"page:{request.Page}:{request.SortBy}:{request.SortDir}";
        return await pageCache.GetOrCreateAsync(key, ct => new ValueTask<PagedResponse<RootCommentResponse>>(LoadPageAsync(request, totalItems, ct)), cancellationToken);
    }

    public async Task<IReadOnlyList<CommentResponse>> GetByIdsAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken)
    {
        var distinctIds = ids.Distinct().ToList();

        var limit = options.Value.MaxIdsPerRequest;
        if (distinctIds.Count > limit)
            throw new TooManyIdsException(limit);

        if (distinctIds.Count == 0)
            return [];

        var comments = await commentCache.GetManyAsync(distinctIds, LoadByIdsAsync, cancellationToken);
        return comments.OrderBy(c => c.Id).ToList();
    }

    public async Task<CommentResponse?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        var comments = await commentCache.GetManyAsync([id], LoadByIdsAsync, cancellationToken);
        return comments.Count == 0 ? null : comments[0];
    }

    private int TotalPages(long totalItems) => (int)((totalItems + options.Value.PageSize - 1) / options.Value.PageSize);

    private async Task<PagedResponse<RootCommentResponse>> LoadPageAsync(GetCommentsRequest request, long totalItems, CancellationToken cancellationToken)
    {
        var pageSize = options.Value.PageSize;
        var totalPages = TotalPages(totalItems);

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

    private async Task<IReadOnlyList<CommentResponse>> LoadByIdsAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken)
    {
        var comments = await repository.GetByIdsAsync(ids, cancellationToken);
        return comments.Select(c => c.ToResponse()).ToList();
    }
}
