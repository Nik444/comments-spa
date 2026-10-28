using System.ComponentModel;

namespace Comments.Api.Application.Responses;

// ImmutableObject: HybridCache отдаёт всем один и тот же объект, а не разбирает сохранённый JSON при каждом чтении.
[ImmutableObject(true)]
public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    long TotalItems,
    int TotalPages);
