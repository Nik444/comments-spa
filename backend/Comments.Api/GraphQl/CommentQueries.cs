using Comments.Api.Application.Exceptions;
using Comments.Api.Application.Models;
using Comments.Api.Application.Requests;
using Comments.Api.Application.Responses;
using Comments.Api.Application.Services;
using HotChocolate;

namespace Comments.Api.GraphQl;

// Тонкий слой над CommentService: ту же логику, кэши и ограничения используют и REST, и GraphQL.
// Поля: comments(page, sortBy, sortDir), commentById(id), commentsByIds(ids). Подробнее в docs/graphql.md.
public sealed class CommentQueries
{
    private const int MaxPage = 1_000_000;

    public async Task<PagedResponse<RootCommentResponse>> GetComments(
        CommentService service,
        CancellationToken cancellationToken,
        int page = 1,
        CommentSortField sortBy = CommentSortField.Date,
        SortDirection sortDir = SortDirection.Desc)
    {
        if (page is < 1 or > MaxPage)
            throw new GraphQLException($"Номер страницы должен быть от 1 до {MaxPage}.");

        return await service.GetPageAsync(new GetCommentsRequest(page, sortBy, sortDir), cancellationToken);
    }

    public Task<CommentResponse?> GetCommentById(CommentService service, CancellationToken cancellationToken, long id) =>
        service.GetByIdAsync(id, cancellationToken);

    public async Task<IReadOnlyList<CommentResponse>> GetCommentsByIds(CommentService service, CancellationToken cancellationToken, List<long> ids)
    {
        try
        {
            return await service.GetByIdsAsync(ids, cancellationToken);
        }
        catch (TooManyIdsException exception)
        {
            throw new GraphQLException(exception.Message);
        }
    }
}
