namespace Comments.Api.Application.Responses;

public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    long TotalItems,
    int TotalPages);
